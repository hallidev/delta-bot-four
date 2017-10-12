using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;
using System.Threading.Tasks;
using RedditSharp;
using Newtonsoft.Json;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentMonitor : ICommentMonitor
    {
        private const string LINK_REPLACE_FROM = "oauth.reddit.com";
        private const string LINK_REPLACE_TO = "www.reddit.com";

        private Reddit _reddit;
        private Subreddit _subreddit;
        private IDB4Queue _queue;

        public CommentMonitor(Reddit reddit, Subreddit subreddit, IDB4Queue queue)
        {
            _reddit = reddit;
            _subreddit = subreddit;
            _queue = queue;
        }

        public async void MonitorForComments()
        {
            // Get all new comments as they are posted
            // This will run as long as the application is running
            await Task.Factory.StartNew(() =>
            {
                var comments = _subreddit.Comments.GetListingStream();

                foreach (var comment in comments)
                {
                    var parent = _reddit.GetThingByFullname(comment.ParentId);

                    var db4Comment = getDB4Comment(comment, parent);

                    if (db4Comment != null) { pushCommentToQueue(db4Comment); }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public async void MonitorForEdits()
        {
            // Get all new comment edits as they are posted
            // This will run as long as the application is running
            await Task.Factory.StartNew(() =>
            {
                var edits = _subreddit.Edited.GetListingStream();

                foreach (var edit in edits)
                {
                    // The edit has to be on a comment
                    if (edit is Comment)
                    {
                        var parent = _reddit.GetThingByFullname(((Comment)edit).ParentId);

                        var db4Comment = getDB4Comment((Comment)edit, parent);

                        if (db4Comment != null) { pushCommentToQueue(db4Comment); }
                    }
                }
            }, TaskCreationOptions.LongRunning);
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
