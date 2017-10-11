using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;
using System;
using System.Threading.Tasks;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentMonitor : ICommentMonitor
    {
        private Subreddit _subreddit;

        public CommentMonitor(Subreddit subreddit)
        {
            _subreddit = subreddit;
        }

        public async void Run()
        {
            await Task.Factory.StartNew(() =>
            {
                var comments = _subreddit.Comments.GetListingStream();

                // Get all new comments as they are posted.
                foreach (Comment comment in comments)
                {
                    Console.WriteLine($"{DateTime.Now}: {comment.Shortlink}");
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
