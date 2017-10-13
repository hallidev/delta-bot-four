using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;
using RedditSharp;
using System.Threading;
using System;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentMonitor : ICommentMonitor
    {
        private Reddit _reddit;
        private Subreddit _subreddit;
        private IObserver<VotableThing> _commentObserver;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public CommentMonitor(Reddit reddit, Subreddit subreddit, IObserver<VotableThing> commentObserver)
        {
            _reddit = reddit;
            _subreddit = subreddit;
            _commentObserver = commentObserver;
        }

        public void Start()
        {
            monitorForComments();
            monitorForEdits();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async void monitorForComments()
        {
            var commentsStream = _subreddit.GetComments().Stream();

            // Get all new comments as they are posted
            // This will run as long as the application is running
            using (commentsStream.Subscribe(_commentObserver))
            {
                await commentsStream.Enumerate(_cancellationTokenSource.Token);
            }
        }

        private async void monitorForEdits()
        {
            var editsStream = _subreddit.GetEdited().Stream();

            // Get all new comments as they are posted
            // This will run as long as the application is running
            using (editsStream.Subscribe(_commentObserver))
            {
                await editsStream.Enumerate(_cancellationTokenSource.Token);
            }
        }
    }
}
