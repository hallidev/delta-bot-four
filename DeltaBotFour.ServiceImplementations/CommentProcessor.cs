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

        public CommentProcessor(AppConfiguration appConfiguration, Subreddit subreddit)
        {
            _appConfiguration = appConfiguration;
            _subreddit = subreddit;
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
            }
        }
    }
}
