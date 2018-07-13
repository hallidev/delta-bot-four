using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;
using DeltaBotFour.Shared.Logging;
using Newtonsoft.Json.Linq;
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

            // For first time runs of DeltaBot, we need to prime the database with a comment id
            // to start at.
            var lastProcessedComments = _db4Repository.GetLastProcessedCommentIds();

            if (lastProcessedComments.Count == 0)
            {
                primeCommentMonitor(lastActivityTimeUtc);
            }

            // For first time runs of DeltaBot, we need to prime the database with an edit id
            // to start at.
            var lastProcessedEdits = _db4Repository.GetLastProcessedEditIds();

            if (lastProcessedEdits.Count == 0)
            {
                primeEditMonitor(lastActivityTimeUtc);
            }

            if (_reddit.User == null)
            {
                Task.Run(async () => await _reddit.InitOrUpdateUserAsync()).Wait();
            }

            // Process unread private messages
            _reddit.User.GetInbox().Where(pm => pm != null && pm.Unread)
                .ForEachAsync(pm => _activityDispatcher.SendToQueue(pm));

            // Process comments since last activity - subtracting commentScanIntervalSeconds
            // will guarantee it runs immediately on startup
            _lastCommentCheckUtc = lastActivityTimeUtc.AddSeconds(-commentScanIntervalSeconds);

            // Start comment monitoring
            monitorComments(commentScanIntervalSeconds);

            // Process edits since last activity - subtracting editScanIntervalSeconds
            // will guarantee it runs immediately on startup
            _lastEditCheckUtc = lastActivityTimeUtc.AddSeconds(-editScanIntervalSeconds);

            // Start edit monitoring
            monitorEdits(editScanIntervalSeconds);

            // Process unread private messages since last activity - subtracting pmScanIntervalSeconds
            // will guarantee monitorPrivateMessages runs immediately on startup
            _lastPMCheckUtc = lastActivityTimeUtc.AddSeconds(-pmScanIntervalSeconds);

            // Start private message monitoring
            monitorPrivateMessages(pmScanIntervalSeconds);
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
                    try
                    {
                        if (DateTime.UtcNow > _lastCommentCheckUtc.AddSeconds(commentScanIntervalSeconds))
                        {
                            // Get the last valid processed comment
                            // This looping / comment checking has to be done
                            // since deleted comments won't return anything
                            var lastCommentIds = _db4Repository.GetLastProcessedCommentIds();

                            string lastProcessedCommentId = string.Empty;

                            foreach (string lastCommentId in lastCommentIds)
                            {
                                if (await isValidComment(lastCommentId))
                                {
                                    lastProcessedCommentId = lastCommentId;
                                    break;
                                }
                            }

                            // If we got here with no commentId, DeltaBot can't continue.
                            if (string.IsNullOrEmpty(lastProcessedCommentId))
                            {
                                _logger.Error(new Exception(), "CRITICAL: No valid last processed comment found. DeltaBot cannot continue monitoring comments...");
                                break;
                            }

                            var commentsJson = await _subreddit.WebAgent.Get($"/r/{_subreddit.Name}/comments.json?before={lastProcessedCommentId}&limit=100");

                            var children = commentsJson["data"]["children"] as JArray;
                            var comments = new List<Comment>();

                            if (children != null && children.Count > 0)
                            {
                                foreach (var child in children)
                                {
                                    comments.Add(Thing.Parse<Comment>(_subreddit.WebAgent, child));
                                }
                            }

                            if (comments.Count > 0)
                            {
                                // Make sure comments are sorted oldest to newest so oldest get processed first
                                var sortedComments = comments.OrderBy(c => c.CreatedUTC).ToList();

                                foreach (var comment in sortedComments)
                                {
                                    // Record the time when this was processed.
                                    // Whenever DeltaBot stops, it's going to read this time
                                    // and query / process all things starting from this time
                                    _db4Repository.SetLastActivityTimeUtc();

                                    // Send to queue for processing
                                    _activityDispatcher.SendToQueue(comment);

                                    // Mark as the last processed comment
                                    _db4Repository.SetLastProcessedCommentId(comment.FullName);
                                }
                            }

                            _lastCommentCheckUtc = DateTime.UtcNow;
                        }

                        Thread.Sleep(100);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error in comment monitor loop - continuing...");
                    }
                }
            }, _cancellationTokenSource.Token);

            _logger.Info("Started monitoring comments...");
        }

        private async void monitorEdits(int editScanIntervalSeconds)
        {
            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (DateTime.UtcNow > _lastEditCheckUtc.AddSeconds(editScanIntervalSeconds))
                        {
                            // Get the last valid processed edit
                            // This looping / edit checking has to be done
                            // since deleted edits won't return anything
                            var lastEditIds = _db4Repository.GetLastProcessedEditIds();

                            string lastProcessedEditId = string.Empty;

                            foreach (string lastEditId in lastEditIds)
                            {
                                if (await isValidComment(lastEditId))
                                {
                                    lastProcessedEditId = lastEditId;
                                    break;
                                }
                            }

                            // If we got here with no editId, DeltaBot can't continue.
                            if (string.IsNullOrEmpty(lastProcessedEditId))
                            {
                                _logger.Error(new Exception(), "CRITICAL: No valid last processed edit found. DeltaBot cannot continue monitoring edits...");
                                break;
                            }

                            var editsJson = await _subreddit.WebAgent.Get($"/r/{_subreddit.Name}/about/edited.json?only=comments&before={lastProcessedEditId}&limit=100");

                            var children = editsJson["data"]["children"] as JArray;
                            var edits = new List<Comment>();

                            if (children != null && children.Count > 0)
                            {
                                foreach (var child in children)
                                {
                                    edits.Add(Thing.Parse<Comment>(_subreddit.WebAgent, child));
                                }
                            }

                            if (edits.Count > 0)
                            {
                                // Make sure comments are sorted oldest to newest so oldest get processed first
                                var sortedEdits = edits.OrderBy(c => c.CreatedUTC).ToList();

                                foreach (var comment in sortedEdits)
                                {
                                    // Record the time when this was processed.
                                    // Whenever DeltaBot stops, it's going to read this time
                                    // and query / process all things starting from this time
                                    _db4Repository.SetLastActivityTimeUtc();

                                    // Send to queue for processing
                                    _activityDispatcher.SendToQueue(comment);

                                    // Mark as the last processed comment
                                    _db4Repository.SetLastProcessedEditId(comment.FullName);
                                }
                            }

                            _lastEditCheckUtc = DateTime.UtcNow;
                        }

                        Thread.Sleep(100);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error in edit monitor loop - continuing...");
                    }
                }
            }, _cancellationTokenSource.Token);

            _logger.Info("Started monitoring edits...");
        }

        private async void monitorPrivateMessages(int pmScanIntervalSeconds)
        {
            await Task.Factory.StartNew(async () =>
            {
                while(true)
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

            _logger.Info("Started monitoring private messages...");
        }

        private void primeCommentMonitor(DateTime lastActivityTimeUtc)
        {
            bool lastCommentSet = false;
            _subreddit.GetComments().Where(c => c.CreatedUTC > lastActivityTimeUtc.AddMinutes(-5))
                .ForEach(comment =>
                {
                    if (!lastCommentSet && isValidComment(comment.FullName).Result)
                    {
                        _db4Repository.SetLastProcessedCommentId(comment.FullName);
                        lastCommentSet = true;
                    }
                });
        }

        private void primeEditMonitor(DateTime lastActivityTimeUtc)
        {
            bool lastEditSet = false;
            _subreddit.GetEdited().Where(c => c.CreatedUTC > lastActivityTimeUtc.AddMinutes(-30))
                .ForEach(edit =>
                {
                    if (!lastEditSet && isValidComment(edit.FullName).Result)
                    {
                        _db4Repository.SetLastProcessedEditId(edit.FullName);
                        lastEditSet = true;
                    }
                });
        }

        private async Task<bool> isValidComment(string fullname)
        {
            var commentJson = await _subreddit.WebAgent.Get($"/r/{_subreddit.Name}/comments.json?limit=1&after={fullname}");
            var children = commentJson["data"]["children"] as JArray;

            return children.Count >= 1;
        }
    }
}
