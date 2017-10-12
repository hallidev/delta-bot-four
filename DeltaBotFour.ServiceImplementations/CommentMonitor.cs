using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;
using System.Threading.Tasks;
using RedditSharp;
using Newtonsoft.Json;
using System.Threading;
using System.Linq;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentMonitor : ICommentMonitor
    {
        private const string LINK_REPLACE_FROM = "oauth.reddit.com";
        private const string LINK_REPLACE_TO = "www.reddit.com";

        private Reddit _reddit;
        private Subreddit _subreddit;
        private IDB4Queue _queue;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public CommentMonitor(Reddit reddit, Subreddit subreddit, IDB4Queue queue)
        {
            _reddit = reddit;
            _subreddit = subreddit;
            _queue = queue;
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
            //// Get all new comments as they are posted
            //// This will run as long as the application is running
            //await _subreddit.GetComments().Stream().ToAsyncEnumerable().ForEachAsync(comment =>
            //{
            //    var parent = _reddit.GetThingByFullnameAsync(comment.ParentId).Result;
            //    //var children = _subreddit.Comments.Take(10).Where(c => c.ParentId == comment.FullName);

            //    var db4Comment = getDB4Comment(comment, parent);

            //    //System.Console.WriteLine(children.ToList().Count);
            //    if (db4Comment != null) { pushCommentToQueue(db4Comment); }
            //});
        }

        private void monitorForEdits()
        {
            //// Get all new comment edits as they are posted
            //// This will run as long as the application is running
            //await Task.Factory.StartNew(() =>
            //{
            //    var edits = _subreddit.GetEdited().Stream().wher;

            //    foreach (var edit in edits)
            //    {
            //        // The edit has to be on a comment
            //        if (edit is Comment)
            //        {
            //            var parent = _reddit.GetThingByFullnameAsync(((Comment)edit).ParentId).Result;

            //            var db4Comment = getDB4Comment((Comment)edit, parent);

            //            if (db4Comment != null) { pushCommentToQueue(db4Comment); }
            //        }
            //    }
            //}, _cancellationTokenSource.Token);
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
