using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Foundation.Helpers;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpActivityMonitor : IActivityMonitor
    {
        private readonly RedditSharp.Reddit _reddit;
        private readonly Subreddit _subreddit;
        private readonly IActivityDispatcher _activityDispatcher;
        private readonly IDB4Repository _db4Repository;
        private readonly IObserver<VotableThing> _commentObserver;
        private readonly IObserver<PrivateMessage> _privateMessageObserver;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public RedditSharpActivityMonitor(RedditSharp.Reddit reddit, Subreddit subreddit, 
            IActivityDispatcher activityDispatcher, IDB4Repository db4Repository)
        {
            _reddit = reddit;
            _subreddit = subreddit;
            _activityDispatcher = activityDispatcher;
            _db4Repository = db4Repository;
            _commentObserver = new IncomingCommentObserver(activityDispatcher);
            _privateMessageObserver = new IncomingPrivateMessageObserver(activityDispatcher);
        }

        public void Start()
        {
            // Get the time of the last processed comment
            var lastProcessedCommentTimeUtc = _db4Repository.GetLastProcessedCommentTimeUtc();

            // Process comments from the last week
            _subreddit.GetComments().Where(c => c.CreatedUTC > lastProcessedCommentTimeUtc)
                .ForEachAsync(c => _activityDispatcher.SendToQueue(c));

            // Start comment monitoring
            monitorComments();

            ConsoleHelper.WriteLine("ActivityMonitor: Started monitoring comments...", ConsoleColor.Green);

            // Start edit monitoring
            monitorEdits();

            ConsoleHelper.WriteLine("ActivityMonitor: Started monitoring edits...", ConsoleColor.Green);

            // Start private message monitoring
            monitorPrivateMessages();

            ConsoleHelper.WriteLine("ActivityMonitor: Started monitoring private messages...", ConsoleColor.Green);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async void monitorComments()
        {
            var commentsStream = _subreddit.GetComments().Stream();

            // Get all new comments as they are posted
            // This will run as long as the application is running
            using (commentsStream.Subscribe(_commentObserver))
            {
                try
                {
                    await commentsStream.Enumerate(_cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    // User cancelled, swallow
                }
                catch (Exception ex)
                {
                    // Make sure no exceptions get thrown out of this method - this will stop the comment monitoring
                    ConsoleHelper.WriteLine(ex.ToString(), ConsoleColor.Red);
                }
            }
        }

        private async void monitorEdits()
        {
            var editsStream = _subreddit.GetEdited().Stream();

            // Get all new comments as they are posted
            // This will run as long as the application is running
            using (editsStream.Subscribe(_commentObserver))
            {
                try
                {
                    await editsStream.Enumerate(_cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    // User cancelled, swallow
                }
                catch (Exception ex)
                {
                    // Make sure no exceptions get thrown out of this method - this will stop the comment monitoring
                    ConsoleHelper.WriteLine(ex.ToString(), ConsoleColor.Red);
                }
            }
        }

        private async void monitorPrivateMessages()
        {
            if (_reddit.User == null)
            {
                await _reddit.InitOrUpdateUserAsync();
            }

            var privateMessageStream = _reddit.User.GetPrivateMessages().Stream();

            // Get all new comments as they are posted
            // This will run as long as the application is running
            using (privateMessageStream.Subscribe(_privateMessageObserver))
            {
                try
                {
                    await privateMessageStream.Enumerate(_cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    // User cancelled, swallow
                }
                catch (Exception ex)
                {
                    // Make sure no exceptions get thrown out of this method - this will stop the comment monitoring
                    ConsoleHelper.WriteLine(ex.ToString(), ConsoleColor.Red);
                }
            }
        }

        private class IncomingCommentObserver : IObserver<VotableThing>
        {
            private readonly IActivityDispatcher _activityDispatcher;

            public IncomingCommentObserver(IActivityDispatcher activityDispatcher)
            {
                _activityDispatcher = activityDispatcher;
            }

            public void OnCompleted()
            {

            }

            public void OnError(Exception error)
            {

            }

            public void OnNext(VotableThing votableThing)
            {
                // We are only observing comments
                if (!(votableThing is Comment comment)) { return; }

                _activityDispatcher.SendToQueue(comment);
            }
        }

        private class IncomingPrivateMessageObserver : IObserver<PrivateMessage>
        {
            private readonly IActivityDispatcher _activityDispatcher;

            public IncomingPrivateMessageObserver(IActivityDispatcher activityDispatcher)
            {
                _activityDispatcher = activityDispatcher;
            }

            public void OnCompleted()
            {
                
            }

            public void OnError(Exception error)
            {
                
            }

            public void OnNext(PrivateMessage privateMessage)
            {
                _activityDispatcher.SendToQueue(privateMessage);
            }
        }
    }
}
