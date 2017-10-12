using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;
using RedditSharp;
using Newtonsoft.Json;
using System.Threading;
using System;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentMonitor : ICommentMonitor
    {
        private Reddit _reddit;
        private Subreddit _subreddit;
        private IObserver<VotableThing> _commentObserver;

        private CancellationToken _cancellationToken = new CancellationToken();

        public CommentMonitor(Reddit reddit, Subreddit subreddit, IObserver<VotableThing> commentObserver)
        {
            _reddit = reddit;
            _subreddit = subreddit;
            _commentObserver = commentObserver;
        }

        public void Run()
        {
            monitorForComments();
            monitorForEdits();
        }

        private async void monitorForComments()
        {
            var commentsStream = _subreddit.GetComments().Stream();

            // Get all new comments as they are posted
            // This will run as long as the application is running
            using (commentsStream.Subscribe(_commentObserver))
            {
                await commentsStream.Enumerate(_cancellationToken);
            }
        }

        private async void monitorForEdits()
        {
            var editsStream = _subreddit.GetEdited().Stream();

            // Get all new comments as they are posted
            // This will run as long as the application is running
            using (editsStream.Subscribe(_commentObserver))
            {
                await editsStream.Enumerate(_cancellationToken);
            }
        }
    }

    public class CommentObserver : IObserver<VotableThing>
    {
        private const string LINK_REPLACE_FROM = "oauth.reddit.com";
        private const string LINK_REPLACE_TO = "www.reddit.com";

        private Reddit _reddit;
        private IDB4Queue _queue;

        public CommentObserver(Reddit reddit, IDB4Queue queue)
        {
            _reddit = reddit;
            _queue = queue;
        }

        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(VotableThing votableThing)
        {
            Comment comment = votableThing as Comment;

            // We are only observing comments
            if(comment == null) { return; }

            var parent = _reddit.GetThingByFullnameAsync(comment.ParentId).Result;
            //var children = _subreddit.Comments.Take(10).Where(c => c.ParentId == comment.FullName);

            var db4Comment = getDB4Comment(comment, parent);

            System.Console.WriteLine("hit");
            if (db4Comment != null)
            {
                pushCommentToQueue(db4Comment);
            }
        }

        private DB4Comment getDB4Comment(Comment comment, Thing parent)
        {
            // If the parent isn't a comment, don't bother continuing
            // It can't contain a valid delta
            if (parent == null || !(parent is Comment))
            {
                return null;
            }

            // Convert to a DB4Comment
            return new DB4Comment
            {
                Id = comment.Id,
                ParentId = comment.ParentId,
                LinkTitle = comment.LinkTitle,
                AuthorName = comment.AuthorName,
                ParentAuthorName = ((Comment)parent).AuthorName,
                ShortLink = comment.Shortlink.Replace(LINK_REPLACE_FROM, LINK_REPLACE_TO),
                Edited = comment.Edited,
                Body = comment.Body
            };
        }

        private void pushCommentToQueue(DB4Comment db4Comment)
        {
            // Put on the queue for comment processing
            _queue.Push(new QueueMessage(QueueMessageType.Comment, JsonConvert.SerializeObject(db4Comment)));
        }
    }
}
