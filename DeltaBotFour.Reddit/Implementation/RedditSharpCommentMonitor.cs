﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Foundation.Helpers;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpCommentMonitor : ICommentMonitor
    {
        private readonly Subreddit _subreddit;
        private readonly ICommentDispatcher _commentDispatcher;
        private readonly IDB4Repository _db4Repository;
        private readonly IObserver<VotableThing> _commentObserver;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public RedditSharpCommentMonitor(Subreddit subreddit, 
            ICommentDispatcher commentDispatcher, IDB4Repository db4Repository)
        {
            _subreddit = subreddit;
            _commentDispatcher = commentDispatcher;
            _db4Repository = db4Repository;
            _commentObserver = new IncomingCommentObserver(commentDispatcher);
        }

        public void Start()
        {
            // Get the time of the last processed comment
            var lastProcessedCommentTimeUtc = _db4Repository.GetLastProcessedCommentTimeUtc();

            // Process comments from the last week
            _subreddit.GetComments().Where(c => c.CreatedUTC > lastProcessedCommentTimeUtc)
                .ForEachAsync(c => _commentDispatcher.SendToQueue(c));

            // Start monitoring
            monitorForComments();

            ConsoleHelper.WriteLine("CommentMonitor: Started monitoring comments...", ConsoleColor.Green);

            monitorForEdits();

            ConsoleHelper.WriteLine("CommentMonitor: Started monitoring edits...", ConsoleColor.Green);
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

        private async void monitorForEdits()
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

        private class IncomingCommentObserver : IObserver<VotableThing>
        {
            private readonly ICommentDispatcher _commentDispatcher;

            public IncomingCommentObserver(ICommentDispatcher commentDispatcher)
            {
                _commentDispatcher = commentDispatcher;
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

                _commentDispatcher.SendToQueue(comment);
            }
        }
    }
}
