using Core.Foundation.Helpers;
using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp;
using System;
using System.Linq;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentProcessor : ICommentProcessor
    {
        private AppConfiguration _appConfiguration;
        private Reddit _reddit;
        private ICommentValidator _commentValidator;
        private ICommentReplyDetector _commentReplyDetector;
        private IDeltaAwarder _deltaAwarder;
        private ICommentReplier _commentReplier;

        public CommentProcessor(AppConfiguration appConfiguration, Reddit reddit,
            ICommentValidator commentValidator, ICommentReplyDetector commentReplyDetector,
            IDeltaAwarder deltaAwarder, ICommentReplier commentReplier)
        {
            _appConfiguration = appConfiguration;
            _reddit = reddit;
            _commentValidator = commentValidator;
            _commentReplyDetector = commentReplyDetector;
            _deltaAwarder = deltaAwarder;
            _commentReplier = commentReplier;
        }

        public void Process(DB4Comment comment)
        {
            // Comments and edits need to be checked for replies and edits.
            // qualifiedComment will have all children populated
            var qualifiedComment = _reddit.GetCommentAsync(new Uri(comment.ShortLink)).Result;
            var parentThing = _reddit.GetThingByFullnameAsync(comment.ParentId).Result;

            // Check for a delta
            if (_appConfiguration.ValidDeltaIndicators.Any(d => comment.Body.Contains(d)))
            {
                string edited = string.Empty;

                if(comment.IsEdited)
                {
                    edited = "(EDITED)";
                }

                ConsoleHelper.WriteLine($"Comment has a delta! {edited}", ConsoleColor.Green);

                // Check to see if db4 has already replied
                var db4ReplyResult = _commentReplyDetector.DidDB4Reply(qualifiedComment);

                // If DB4 hasn't replied, or if it did but this is an edit, perform comment logic
                if (!db4ReplyResult.HasDB4Replied)
                {
                    // Validate comment
                    var commentValidationResult = _commentValidator.Validate(qualifiedComment, parentThing);

                    if (commentValidationResult.IsValidDelta)
                    {
                        // Award the delta
                        _deltaAwarder.Award(qualifiedComment);
                    }

                    // Post a reply with the result
                    _commentReplier.Reply(qualifiedComment, commentValidationResult);

                    ConsoleHelper.WriteLine($"DeltaBot replied -> result: {commentValidationResult.ResultType.ToString()} link: {qualifiedComment.Shortlink}");
                }
                else
                {
                    // DB4 already replied. If DB4's reply was a fail reply, check to see if this delta
                    // now passes validation. If it does, edit the old reply to be a success reply
                    if(!db4ReplyResult.WasSuccessReply)
                    {
                        // Validate comment
                        var commentValidationResult = _commentValidator.Validate(qualifiedComment, parentThing);

                        if (commentValidationResult.IsValidDelta)
                        {
                            // Award the delta
                            _deltaAwarder.Award(qualifiedComment);
                        }

                        // Edit the result to reflect new delta comment
                        _commentReplier.EditReply(db4ReplyResult.Comment, commentValidationResult);

                        ConsoleHelper.WriteLine($"DeltaBot edited a reply -> result: {commentValidationResult.ResultType.ToString()} link: {qualifiedComment.Shortlink}");
                    }
                }
            }
            else if(comment.IsEdited)
            {
                // There is no delta. Check if DB4 replied. This means that
                // there was a delta previously. If the comment is less than HoursToRemoveDelta hours old, the delta
                // can be removed.

                // Check to see if db4 has replied
                var db4ReplyResult = _commentReplyDetector.DidDB4Reply(qualifiedComment);

                // If DB4 replied and awarded a delta in the last HoursToUnawardDelta, unaward it
                if (db4ReplyResult.HasDB4Replied && db4ReplyResult.WasSuccessReply && qualifiedComment.CreatedUTC < DateTime.Now.AddHours(-_appConfiguration.HoursToUnawardDelta))
                {
                    // Unaward
                    _deltaAwarder.Unaward(qualifiedComment);

                    // Delete award comment
                    _commentReplier.DeleteReply(db4ReplyResult.Comment);

                    ConsoleHelper.WriteLine($"DeltaBot unawarded and deleted a reply -> link: {qualifiedComment.Shortlink}");
                }
            }
        }
    }
}
