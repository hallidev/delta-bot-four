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
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private DateTime _lastCommentCheckUtc;
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
        }

        public void Start(int commentScanIntervalSeconds, int editScanIntervalSeconds, int pmScanIntervalSeconds)
        {
            // Get the time of the last processed comment
            var lastActivityTimeUtc = _db4Repository.GetLastActivityTimeUtc();

            // Process comments since last activity - subtracting commentScanIntervalSeconds
            // will guarantee monitorComments runs immediately on startup
            _lastCommentCheckUtc = lastActivityTimeUtc.AddSeconds(-commentScanIntervalSeconds);

            // Process edits since last activity - subtracting editScanIntervalSeconds
            // will guarantee monitorEdits runs immediately on startup
            _lastEditCheckUtc = lastActivityTimeUtc.AddSeconds(-editScanIntervalSeconds);

            if (_reddit.User == null)
            {
                Task.Run(async () => await _reddit.InitOrUpdateUserAsync()).Wait();
            }

            // Process unread private messages since last activity - subtracting pmScanIntervalSeconds
            // will guarantee monitorPrivateMessages runs immediately on startup
            _lastPMCheckUtc = lastActivityTimeUtc.AddSeconds(-commentScanIntervalSeconds);

            // Start comment monitoring
            monitorComments(commentScanIntervalSeconds);

            _logger.Info("Started monitoring comments...");

            // Start edit monitoring
            monitorEdits(editScanIntervalSeconds);

            _logger.Info("Started monitoring edits...");

            // Start private message monitoring
            monitorPrivateMessages(pmScanIntervalSeconds);

            _logger.Info("Started monitoring private messages...");
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async void monitorComments(int commentScanIntervalSeconds)
        {
            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (DateTime.UtcNow > _lastCommentCheckUtc.AddSeconds(commentScanIntervalSeconds))
                    {
                        // Process comments since last activity
                        await _subreddit.GetComments().Where(c => c != null && c.CreatedUTC > _lastCommentCheckUtc)
                            .ForEachAsync(c =>
                            {
                                if (c is Comment comment)
                                {
                                    _activityDispatcher.SendToQueue(comment);
                                }
                            });

                        _lastCommentCheckUtc = DateTime.UtcNow;

                        // Record the time when this happened.
                        // Whenever DeltaBot stops, it's going to read this time
                        // and query / process all things starting from this time
                        _db4Repository.SetLastActivityTimeUtc();
                    }

                    Thread.Sleep(100);
                }
            }, _cancellationTokenSource.Token);
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
                        await _subreddit.GetEdited().Where(c => c != null && c.EditedUTC.HasValue && c.EditedUTC > _lastEditCheckUtc)
                            .ForEachAsync(c =>
                            {
                                if (c is Comment editedComment)
                                {
                                    _activityDispatcher.SendToQueue(editedComment);
                                }
                            });

                        _lastEditCheckUtc = DateTime.UtcNow;

                        // Record the time when this happened.
                        // Whenever DeltaBot stops, it's going to read this time
                        // and query / process all things starting from this time
                        _db4Repository.SetLastActivityTimeUtc();
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
                        await _reddit.User.GetInbox().Where(pm => pm != null && pm.Unread)
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
    }
}
