using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;
using RedditSharp;
using System.Threading;
using System;
using System.Linq;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentMonitor : ICommentMonitor
    {
        private Reddit _reddit;
        private Subreddit _subreddit;
        private IObserver<VotableThing> _commentObserver;
        private ICommentDispatcher _commentDispatcher;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public CommentMonitor(Reddit reddit, Subreddit subreddit, IObserver<VotableThing> commentObserver,
            ICommentDispatcher commentDispatcher)
        {
            _reddit = reddit;
            _subreddit = subreddit;
            _commentObserver = commentObserver;
            _commentDispatcher = commentDispatcher;
        }

        public void Start()
        {
            // Process comments from the last week
            _subreddit.GetComments().Take(500).Where(c => c.Created > DateTime.Now.AddDays(-7))
                .ForEachAsync(c => _commentDispatcher.SendToQueue(c));

            // Start monitoring
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
