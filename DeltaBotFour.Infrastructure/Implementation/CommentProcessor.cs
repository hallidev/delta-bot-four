using System;
using System.Linq;
using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class CommentProcessor : ICommentProcessor
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IRedditService _redditService;
        private readonly ICommentValidator _commentValidator;
        private readonly ICommentReplyDetector _commentReplyDetector;
        private readonly IDeltaAwarder _deltaAwarder;
        private readonly ICommentReplier _commentReplier;
        private readonly IDB4Repository _db4Repository;

        public CommentProcessor(AppConfiguration appConfiguration, IRedditService redditService,
            ICommentValidator commentValidator, ICommentReplyDetector commentReplyDetector,
            IDeltaAwarder deltaAwarder, ICommentReplier commentReplier, IDB4Repository db4Repository)
        {
            _appConfiguration = appConfiguration;
            _redditService = redditService;
            _commentValidator = commentValidator;
            _commentReplyDetector = commentReplyDetector;
            _deltaAwarder = deltaAwarder;
            _commentReplier = commentReplier;
            _db4Repository = db4Repository;
        }

        public void Process(DB4Thing comment)
        {
            // If we got here with a PM or post, that's a problem
            Assert.That(comment.Type == DB4ThingType.Comment, $"CommentProcessor received type: {comment.Type}");

            // DB4 shouldn't process its own comments
            if (comment.AuthorName == _appConfiguration.DB4Username)
            {
                return;
            }

            // Check for a delta
            bool hasDelta = commentHasDelta(comment.Body, out bool hadDeltaInQuotes);

            // If there was no legitimate delta, but there was a delta in quotes, PM
            // the user to let them know in case they made a mistake with reddit quoting
            if (!hasDelta && hadDeltaInQuotes && !_db4Repository.GetIgnoreQuotedDeltaPMUserList().Contains(comment.AuthorName))
            {
                string subject = _appConfiguration.PrivateMessages.DeltaInQuoteSubject;
                string body = _appConfiguration.PrivateMessages.DeltaInQuoteMessage
                    .Replace(_appConfiguration.ReplaceTokens.UsernameToken, comment.AuthorName)
                    .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName);

                _redditService.SendPrivateMessage(subject, body, comment.AuthorName);
            }

            if (hasDelta || comment.IsEdited)
            {
                // There is a delta or this comment is edited
                // We need to get more info for processing
                _redditService.PopulateParentAndChildren(comment);

                if (hasDelta)
                {
                    // Check to see if db4 has already replied
                    var db4ReplyResult = _commentReplyDetector.DidDB4Reply(comment);

                    // If DB4 hasn't replied, or if it did but this is an edit, perform comment logic
                    if (!db4ReplyResult.HasDB4Replied)
                    {
                        // Validate comment and award delta if successful
                        var commentValidationResult = validateAndAward(comment);

                        // Post a reply with the result
                        _commentReplier.Reply(comment, commentValidationResult);
                    }
                    else
                    {
                        // DB4 already replied. If DB4's reply was a fail reply, check to see if this delta
                        // now passes validation. If it does, edit the old reply to be a success reply
                        if (!db4ReplyResult.WasSuccessReply)
                        {
                            // Validate comment and award delta if successful
                            var commentValidationResult = validateAndAward(comment);

                            // Edit the result to reflect new delta comment
                            _commentReplier.EditReply(db4ReplyResult.Comment, commentValidationResult);
                        }
                    }
                }
                else if (comment.IsEdited)
                {
                    // There is no delta. Check if DB4 replied. This means that
                    // there was a delta previously. If the comment is less than HoursToRemoveDelta hours old, the delta
                    // can be removed.

                    // Check to see if db4 has replied
                    var db4ReplyResult = _commentReplyDetector.DidDB4Reply(comment);

                    // If DB4 replied and awarded a delta in the last HoursToUnawardDelta, unaward it
                    if (db4ReplyResult.HasDB4Replied && db4ReplyResult.WasSuccessReply && comment.CreatedUTC < DateTime.Now.AddHours(-_appConfiguration.HoursToUnawardDelta))
                    {
                        // Unaward
                        // parentThing can safely be cast to Comment here - we could have only
                        // gotten here if a delta was previously awarded, meaning the parent of this
                        // Comment is a Comment also
                        Assert.That(comment.ParentThing.Type == DB4ThingType.Comment);
                        _deltaAwarder.Unaward(comment);

                        // Delete award comment
                        _commentReplier.DeleteReply(db4ReplyResult.Comment);
                    }
                }
            }
        }

        private bool commentHasDelta(string commentBody, out bool hadDeltaInQuotes)
        {
            hadDeltaInQuotes = false;

                // First split the comment up on newlines
                var commentLines = commentBody.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            );

            // For each line that isn't a reddit quote, check for a delta
            foreach (var commentLine in commentLines)
            {
                if (_appConfiguration.ValidDeltaIndicators.Any(d => commentLine.Contains(d)))
                {
                    if (!commentLine.StartsWith("&gt;"))
                    {
                        // This line has a delta that wasn't in a quote - it's good
                        return true;
                    }

                    // Line had a quoted delta
                    hadDeltaInQuotes = true;
                }
            }

            return false;
        }

        private DeltaCommentValidationResult validateAndAward(DB4Thing qualifiedComment)
        {
            // Validate comment
            var commentValidationResult = _commentValidator.Validate(qualifiedComment);

            if (commentValidationResult.IsValidDelta)
            {
                // Award the delta
                // parentThing must be a Comment here - deltas are only
                // valid when the parent is a Comment
                Assert.That(qualifiedComment.ParentThing.Type == DB4ThingType.Comment);

                _deltaAwarder.Award(qualifiedComment);
            }

            return commentValidationResult;
        }
    }
}
