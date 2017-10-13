using Core.Foundation.Helpers;
using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;
using System;
using System.Linq;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentProcessor : ICommentProcessor
    {
        private AppConfiguration _appConfiguration;
        private Subreddit _subreddit;
        private ICommentValidator _commentValidator;
        private IDeltaAwarder _deltaAwarder;
        private ICommentReplier _commentReplier;

        public CommentProcessor(AppConfiguration appConfiguration, Subreddit subreddit,
            ICommentValidator commentValidator, IDeltaAwarder deltaAwarder, ICommentReplier commentReplier)
        {
            _appConfiguration = appConfiguration;
            _subreddit = subreddit;
            _commentValidator = commentValidator;
            _deltaAwarder = deltaAwarder;
            _commentReplier = commentReplier;
        }

        public void Process(DB4Comment comment)
        {
            // DB4 doesn't qualify
            if(comment.AuthorName == _appConfiguration.DB4Username) { return; }

            // Check for a delta
            if (_appConfiguration.ValidDeltaIndicators.Any(d => comment.Body.Contains(d)))
            {
                string edited = string.Empty;

                if(comment.Edited)
                {
                    edited = "EDITED ";
                }

                ConsoleHelper.WriteLine($"{edited}Comment has a delta!", ConsoleColor.Green);
                ConsoleHelper.WriteLine($"Comment: {comment.Body}");

                // Validate comment
                var commentValidationResult = _commentValidator.Validate(comment);

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
