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

        public CommentProcessor(AppConfiguration appConfiguration, Subreddit subreddit)
        {
            _appConfiguration = appConfiguration;
            _subreddit = subreddit;
        }

        public void Process(DB4Comment comment)
        {
            Console.WriteLine("Comment!");

            // DB4 doesn't qualify
            if(comment.AuthorName == _appConfiguration.DB4Username) { return; }

            // Check for a delta
            if (_appConfiguration.ValidDeltaIndicators.Any(d => comment.Body.Contains(d)))
            {
                Console.WriteLine($"Comment has a delta!\r\nComment: {comment.Body}");
            }
        }
    }
}
