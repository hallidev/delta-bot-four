using Core.Foundation.Helpers;
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
        private AppConfiguration _appConfiguration;
        private Reddit _reddit;
        private ICommentValidator _commentValidator;
        private IDeltaAwarder _deltaAwarder;
        private ICommentReplier _commentReplier;

        public CommentProcessor(AppConfiguration appConfiguration, Reddit reddit,
            ICommentValidator commentValidator, IDeltaAwarder deltaAwarder, ICommentReplier commentReplier)
        {
            _appConfiguration = appConfiguration;
            _reddit = reddit;
            _commentValidator = commentValidator;
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

                // Validate comment
                var commentValidationResult = _commentValidator.Validate(qualifiedComment, parentThing);

                if(commentValidationResult.IsValidDelta)
                {
                    // Award the delta
                    _deltaAwarder.Award(comment);
                }

                // Post a reply with the result
                _commentReplier.Reply(comment, commentValidationResult);
            }
        }
    }
}
