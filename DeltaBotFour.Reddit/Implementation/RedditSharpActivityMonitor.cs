using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;
using DeltaBotFour.Shared;
using DeltaBotFour.Shared.Logging;
using Newtonsoft.Json.Linq;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation;

public class RedditSharpActivityMonitor : IActivityMonitor
{
    private readonly IActivityDispatcher _activityDispatcher;
    private readonly AutoRestartManager _autoRestartManager;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IDB4Repository _db4Repository;
    private readonly ILogger _logger;
    private readonly RedditSharp.Reddit _reddit;
    private readonly Subreddit _subreddit;

    private DateTime _lastCommentCheckUtc;
    private DateTime _lastEditCheckUtc;
    private DateTime _lastPMCheckUtc;

    public RedditSharpActivityMonitor(RedditSharp.Reddit reddit,
        Subreddit subreddit,
        AutoRestartManager autoRestartManager,
        IActivityDispatcher activityDispatcher,
        IDB4Repository db4Repository,
        ILogger logger)
    {
        _reddit = reddit;
        _subreddit = subreddit;
        _autoRestartManager = autoRestartManager;
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

        if (lastProcessedComments.Count == 0) primeCommentMonitor(lastActivityTimeUtc);

        // For first time runs of DeltaBot, we need to prime the database with an edit id
        // to start at.
        var lastProcessedEdits = _db4Repository.GetLastProcessedEditIds();

        if (lastProcessedEdits.Count == 0) primeEditMonitor(lastActivityTimeUtc);

        if (_reddit.User == null) Task.Run(async () => await _reddit.InitOrUpdateUserAsync()).Wait();

        // Process comments since last activity - subtracting commentScanIntervalSeconds
        // will guarantee it runs immediately on startup
        _lastCommentCheckUtc = lastActivityTimeUtc.AddSeconds(-commentScanIntervalSeconds);

        // Start checking for auto restart
        monitorAutoRestart(1);

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

    private async void monitorAutoRestart(int autoRestartCheckSeconds)
    {
        await Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                _autoRestartManager.RestartIfNecessary();
                await Task.Delay(TimeSpan.FromSeconds(autoRestartCheckSeconds));
            }
        }, _cancellationTokenSource.Token);
    }

    private async void monitorComments(int commentScanIntervalSeconds)
    {
        await Task.Factory.StartNew(async () =>
        {
            while (true)
                try
                {
                    if (DateTime.UtcNow > _lastCommentCheckUtc.AddSeconds(commentScanIntervalSeconds))
                    {
                        // Get the last valid processed comment
                        // This looping / comment checking has to be done
                        // since deleted comments won't return anything
                        var lastCommentIds = _db4Repository.GetLastProcessedCommentIds();

                        var lastProcessedCommentId = string.Empty;

                        foreach (var lastCommentId in lastCommentIds)
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
                            var latestComments = await _subreddit.GetComments(1).OrderByDescending(c => c.CreatedUTC)
                                .ToListAsync();

                            if (latestComments != null && latestComments.Count > 0)
                            {
                                lastProcessedCommentId = latestComments[0].FullName;
                                // Need to have a good starting point ASAP
                                _db4Repository.SetLastProcessedCommentId(lastProcessedCommentId);
                                _logger.Warn($"WARN: Had to bootstrap from latest comment: {lastProcessedCommentId}!!");
                            }
                            else
                            {
                                _logger.Error(new Exception(),
                                    "CRITICAL: No valid last processed comment found. DeltaBot cannot continue monitoring comments...");
                                break;
                            }
                        }

                        var commentsJson = await _subreddit.WebAgent.Get(
                            $"/r/{_subreddit.Name}/comments.json?before={lastProcessedCommentId}&limit=100");

                        var children = commentsJson["data"]["children"] as JArray;
                        var comments = new List<Comment>();

                        if (children != null && children.Count > 0)
                            foreach (var child in children)
                                comments.Add(Thing.Parse<Comment>(_subreddit.WebAgent, child));

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
        }, _cancellationTokenSource.Token);

        _logger.Info("Started monitoring comments...");
    }

    private async void monitorEdits(int editScanIntervalSeconds)
    {
        await Task.Factory.StartNew(async () =>
        {
            while (true)
                try
                {
                    if (DateTime.UtcNow > _lastEditCheckUtc.AddSeconds(editScanIntervalSeconds))
                    {
                        // Get the last valid processed edit
                        // This looping / edit checking has to be done
                        // since deleted edits won't return anything
                        var lastEditIds = _db4Repository.GetLastProcessedEditIds();

                        var lastProcessedEditId = string.Empty;

                        foreach (var lastEditId in lastEditIds)
                            if (await isValidComment(lastEditId))
                            {
                                lastProcessedEditId = lastEditId;
                                break;
                            }

                        // If we got here with no editId, DeltaBot can't continue.
                        if (string.IsNullOrEmpty(lastProcessedEditId))
                        {
                            var latestEdits =
                                await _subreddit.GetEdited(1).OrderByDescending(c => c.CreatedUTC).ToListAsync();

                            if (latestEdits != null && latestEdits.Count > 0)
                            {
                                lastProcessedEditId = latestEdits[0].FullName;
                                // Need to have a good starting point ASAP
                                _db4Repository.SetLastProcessedEditId(lastProcessedEditId);
                                _logger.Warn($"WARN: Had to bootstrap from latest edit: {lastProcessedEditId}!!");
                            }
                            else
                            {
                                _logger.Error(new Exception(),
                                    "CRITICAL: No valid last processed edit found. DeltaBot cannot continue monitoring edits...");
                                break;
                            }
                        }

                        var editsJson = await _subreddit.WebAgent.Get(
                            $"/r/{_subreddit.Name}/about/edited.json?only=comments&before={lastProcessedEditId}&limit=100");

                        var children = editsJson["data"]["children"] as JArray;
                        var edits = new List<Comment>();

                        if (children != null && children.Count > 0)
                            foreach (var child in children)
                                edits.Add(Thing.Parse<Comment>(_subreddit.WebAgent, child));

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

                                // Mark as the last processed edit
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
        }, _cancellationTokenSource.Token);

        _logger.Info("Started monitoring edits...");
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
                    await _reddit.User.GetUnreadMessages(mark: false)
                        .ForEachAsync(pm =>
                        {
                            if (pm is PrivateMessage privateMessage) _activityDispatcher.SendToQueue(privateMessage);
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
        var lastCommentSet = false;
        _subreddit.GetComments().Where(c => c.CreatedUTC > lastActivityTimeUtc.AddMinutes(-5))
            .ForEachAsync(comment =>
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
        var lastEditSet = false;
        _subreddit.GetEdited().Where(c => c.CreatedUTC > lastActivityTimeUtc.AddMinutes(-30))
            .ForEachAsync(edit =>
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