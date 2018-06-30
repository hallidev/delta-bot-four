using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;
using DeltaBotFour.Shared.Logging;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpActivityMonitor : IActivityMonitor
    {
        private readonly RedditSharp.Reddit _reddit;
        private readonly Subreddit _subreddit;
        private readonly IActivityDispatcher _activityDispatcher;
        private readonly IDB4Repository _db4Repository;
        private readonly ILogger _logger;
        private readonly IObserver<VotableThing> _commentObserver;
        private readonly IObserver<PrivateMessage> _privateMessageObserver;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private DateTime _lastEditCheckUtc;

        public RedditSharpActivityMonitor(RedditSharp.Reddit reddit,
            Subreddit subreddit,
            IActivityDispatcher activityDispatcher,
            IDB4Repository db4Repository,
            ILogger logger)
        {
            _reddit = reddit;
            _subreddit = subreddit;
            _activityDispatcher = activityDispatcher;
            _db4Repository = db4Repository;
            _logger = logger;
            _commentObserver = new IncomingCommentObserver(activityDispatcher, db4Repository, logger);
            _privateMessageObserver = new IncomingPrivateMessageObserver(activityDispatcher, db4Repository, logger);
        }

        public void Start(int editScanIntervalSeconds)
        {
            // Get the time of the last processed comment
            var lastActivityTimeUtc = _db4Repository.GetLastActivityTimeUtc();

            // Process comments since last activity
            _subreddit.GetComments().Where(c => c.CreatedUTC > lastActivityTimeUtc)
                .ForEachAsync(c => _activityDispatcher.SendToQueue(c));

            // Process edits since last activity - subtracting editScanIntervalSeconds
            // will guarantee it runs immediately on startup
            _lastEditCheckUtc = lastActivityTimeUtc.AddSeconds(-editScanIntervalSeconds);

            if (_reddit.User == null)
            {
                Task.Run(async () => await _reddit.InitOrUpdateUserAsync()).Wait();
            }

            // Process unread private messages
            _reddit.User.GetInbox().Where(pm => pm != null && pm.Unread)
                .ForEachAsync(pm => _activityDispatcher.SendToQueue(pm));

            // Start comment monitoring
            monitorComments();

            _logger.Info("Started monitoring comments...");

            // Start edit monitoring
            monitorEdits(editScanIntervalSeconds);

            _logger.Info("Started monitoring edits...");

            // Start private message monitoring
            monitorPrivateMessages();

            _logger.Info("Started monitoring private messages...");
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
                    _logger.Error(ex);
                }
            }
        }

        private async void monitorEdits(int editScanIntervalSeconds)
        {
            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (DateTime.UtcNow > _lastEditCheckUtc.AddSeconds(editScanIntervalSeconds))
                    {
                        // Process edits since last activity
                        await _subreddit.GetEdited().Where(c => c.EditedUTC.HasValue && c.EditedUTC > _lastEditCheckUtc)
                            .ForEachAsync(c =>
                            {
                                if (c is Comment editedComment)
                                {
                                    _activityDispatcher.SendToQueue(editedComment);
                                }
                            });

                        _lastEditCheckUtc = DateTime.UtcNow;
                    }

                    Thread.Sleep(100);
                }
            }, _cancellationTokenSource.Token);
        }

        private async void monitorPrivateMessages()
        {
            var privateMessageStream = _reddit.User.GetInbox().Stream();

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
                    _logger.Error(ex);
                }
            }
        }

        private class IncomingCommentObserver : IObserver<VotableThing>
        {
            private readonly IActivityDispatcher _activityDispatcher;
            private readonly IDB4Repository _db4Repository;
            private readonly ILogger _logger;

            public IncomingCommentObserver(IActivityDispatcher activityDispatcher,
                IDB4Repository db4Repository, ILogger logger)
            {
                _activityDispatcher = activityDispatcher;
                _db4Repository = db4Repository;
                _logger = logger;
            }

            public void OnCompleted()
            {

            }

            public void OnError(Exception error)
            {
                _logger.Error(error, "IncomingCommentObserver:OnError");
            }

            public void OnNext(VotableThing votableThing)
            {
                // We are only observing comments
                if (!(votableThing is Comment comment)) { return; }

                // There is a known bug in RedditSharp where this observer can attempt
                // to process a few very old comments where it starts up. I'm arbitrarily picking 6 months
                // here. I'm not sure if there's a better way to fix this
                if ((DateTime.UtcNow - comment.CreatedUTC).TotalDays > 180)
                {
                    return;
                }

                // Record the time when this was processed.
                // Whenever DeltaBot stops, it's going to read this time
                // and query / process all things starting from this time
                _db4Repository.SetLastActivityTimeUtc();

                // Send to queue for processing
                _activityDispatcher.SendToQueue(comment);
            }
        }

        private class IncomingPrivateMessageObserver : IObserver<PrivateMessage>
        {
            private readonly IActivityDispatcher _activityDispatcher;
            private readonly IDB4Repository _db4Repository;
            private readonly ILogger _logger;

            public IncomingPrivateMessageObserver(IActivityDispatcher activityDispatcher,
                IDB4Repository db4Repository, ILogger logger)
            {
                _activityDispatcher = activityDispatcher;
                _db4Repository = db4Repository;
                _logger = logger;
            }

            public void OnCompleted()
            {

            }

            public void OnError(Exception error)
            {
                _logger.Error(error, "IncomingPrivateMessageObserver:OnError");
            }

            public void OnNext(PrivateMessage privateMessage)
            {
                // Record the time when this was processed.
                // Whenever DeltaBot stops, it's going to read this time
                // and query / process all things starting from this time
                _db4Repository.SetLastActivityTimeUtc();

                // Only send unread messages to queue for processing
                if (privateMessage != null && privateMessage.Unread)
                {
                    _activityDispatcher.SendToQueue(privateMessage);
                }
            }
        }
    }
}
