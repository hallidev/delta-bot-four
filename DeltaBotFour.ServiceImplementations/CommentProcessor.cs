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
            // Check for a delta
            if (_appConfiguration.ValidDeltaIndicators.Any(d => comment.Body.Contains(d)))
            {
                string edited = string.Empty;

                if(comment.IsEdited)
                {
                    edited = "EDITED ";
                }

                ConsoleHelper.WriteLine($"{edited}Comment has a delta!", ConsoleColor.Green);
                ConsoleHelper.WriteLine($"Comment: {comment.Body}");

                // Comments with a delta need to have parent and children
                // validated. Retrieve fully qualified comment
                var qualifiedComment = _reddit.GetCommentAsync(new Uri(comment.ShortLink)).Result;
                var parentThing = _reddit.GetThingByFullnameAsync(comment.ParentId).Result;

                if(_commentReplyDetector.DidDB4Reply(qualifiedComment).DidDB4Reply)
                {
                    // DB4 has already replied, move on
                    return;
                }

                // Validate comment
                var commentValidationResult = _commentValidator.Validate(qualifiedComment, parentThing);

                if(commentValidationResult.IsValidDelta)
                {
                    // Award the delta
                    _deltaAwarder.Award(qualifiedComment);
                }

                // Post a reply with the result
                _commentReplier.Reply(qualifiedComment, commentValidationResult);
            }
        }
    }
}
