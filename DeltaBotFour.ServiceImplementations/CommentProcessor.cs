using Core.Foundation.Helpers;
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

        public void Process(Models.CommentComposite commentComposite)
        {
            // DB4 doesn't qualify
            if(commentComposite.Comment.AuthorName == _appConfiguration.DB4Username) { return; }

            // Check for a delta
            if (_appConfiguration.ValidDeltaIndicators.Any(d => commentComposite.Comment.Body.Contains(d)))
            {
                string edited = string.Empty;

                if(commentComposite.Comment.Edited)
                {
                    edited = "EDITED ";
                }

                ConsoleHelper.WriteLine($"{edited}Comment has a delta!", ConsoleColor.Green);
                ConsoleHelper.WriteLine($"Comment: {commentComposite.Comment.Body}");

                // Validate comment
                var commentValidationResult = _commentValidator.Validate(commentComposite);

                if(commentValidationResult.IsValidDelta)
                {
                    // Award the delta
                    _deltaAwarder.Award(commentComposite);
                }

                // Post a reply with the result
                _commentReplier.Reply(commentComposite, commentValidationResult);
            }
        }
    }
}
