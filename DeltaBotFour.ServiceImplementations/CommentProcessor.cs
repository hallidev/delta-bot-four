using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Linq;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentProcessor : ICommentProcessor
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly Reddit _reddit;
        private readonly ICommentValidator _commentValidator;
        private readonly ICommentReplyDetector _commentReplyDetector;
        private readonly IDeltaAwarder _deltaAwarder;
        private readonly ICommentReplier _commentReplier;

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
            // Check for a delta
            bool hasDeltas = _appConfiguration.ValidDeltaIndicators.Any(d => comment.Body.Contains(d));

            if (hasDeltas || comment.IsEdited)
            {
                // Comments and edits need to be checked for replies and edits.
                // qualifiedComment will have all children populated
                var qualifiedComment = _reddit.GetCommentAsync(new Uri(comment.ShortLink)).Result;
                var parentThing = _reddit.GetThingByFullnameAsync(comment.ParentId).Result;

                if (hasDeltas)
                {
                    // Check to see if db4 has already replied
                    var db4ReplyResult = _commentReplyDetector.DidDB4Reply(qualifiedComment);

                    // If DB4 hasn't replied, or if it did but this is an edit, perform comment logic
                    if (!db4ReplyResult.HasDB4Replied)
                    {
                        // Validate comment and award delta if successful
                        var commentValidationResult = validateAndAward(qualifiedComment, parentThing);

                        // Post a reply with the result
                        _commentReplier.Reply(qualifiedComment, commentValidationResult);
                    }
                    else
                    {
                        // DB4 already replied. If DB4's reply was a fail reply, check to see if this delta
                        // now passes validation. If it does, edit the old reply to be a success reply
                        if (!db4ReplyResult.WasSuccessReply)
                        {
                            // Validate comment and award delta if successful
                            var commentValidationResult = validateAndAward(qualifiedComment, parentThing);

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
                    var db4ReplyResult = _commentReplyDetector.DidDB4Reply(qualifiedComment);

                    // If DB4 replied and awarded a delta in the last HoursToUnawardDelta, unaward it
                    if (db4ReplyResult.HasDB4Replied && db4ReplyResult.WasSuccessReply && qualifiedComment.CreatedUTC < DateTime.Now.AddHours(-_appConfiguration.HoursToUnawardDelta))
                    {
                        // Unaward
                        // parentThing can safely be cast to Comment here - we could have only
                        // gotten here if a delta was previously awarded, meaning the parent of this
                        // Comment is a Comment also
                        _deltaAwarder.Unaward(qualifiedComment, (Comment)parentThing);

                        // Delete award comment
                        _commentReplier.DeleteReply(db4ReplyResult.Comment);
                    }
                }
            }
        }

        private DeltaCommentValidationResult validateAndAward(Comment qualifiedComment, Thing parentThing)
        {
            // Validate comment
            var commentValidationResult = _commentValidator.Validate(qualifiedComment, parentThing);

            if (commentValidationResult.IsValidDelta)
            {
                // Award the delta
                // parentThing can safely be cast to Comment here - deltas are only
                // valid when the parent is a Comment
                _deltaAwarder.Award(qualifiedComment, (Comment)parentThing);
            }

            return commentValidationResult;
        }
    }
}
