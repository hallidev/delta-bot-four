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
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private DateTime _lastEditCheckUtc;
        private DateTime _lastPMCheckUtc;

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
        }

        public void Start(int editScanIntervalSeconds, int pmScanIntervalSeconds)
        {
            // Get the time of the last processed comment
            var lastActivityTimeUtc = _db4Repository.GetLastActivityTimeUtc();

            // Process comments since last activity
            _subreddit.GetComments().Where(c => c.CreatedUTC > lastActivityTimeUtc)
                .ForEachAsync(c => _activityDispatcher.SendToQueue(c));

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

            // Process edits since last activity - subtracting editScanIntervalSeconds
            // will guarantee it runs immediately on startup
            _lastEditCheckUtc = lastActivityTimeUtc.AddSeconds(-editScanIntervalSeconds);

            // Start edit monitoring
            monitorEdits(editScanIntervalSeconds);

            _logger.Info("Started monitoring edits...");

            // Process unread private messages since last activity - subtracting pmScanIntervalSeconds
            // will guarantee monitorPrivateMessages runs immediately on startup
            _lastPMCheckUtc = lastActivityTimeUtc.AddSeconds(-pmScanIntervalSeconds);

            // Start private message monitoring
            monitorPrivateMessages(pmScanIntervalSeconds);

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

        private async void monitorPrivateMessages(int pmScanIntervalSeconds)
        {
            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (DateTime.UtcNow > _lastPMCheckUtc.AddSeconds(pmScanIntervalSeconds))
                    {
                        // Process private messages since last activity
                        await _reddit.User.GetUnreadMessages()
                            .ForEachAsync(pm =>
                            {
                                if (pm is PrivateMessage privateMessage)
                                {
                                    _activityDispatcher.SendToQueue(privateMessage);
                                }
                            });

                        _lastPMCheckUtc = DateTime.UtcNow;

                        // Record the time when this happened.
                        // Whenever DeltaBot stops, it's going to read this time
                        // and query / process all things starting from this time
                        _db4Repository.SetLastActivityTimeUtc();
                    }

                    Thread.Sleep(100);
                }
            }, _cancellationTokenSource.Token);
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
    }
}
