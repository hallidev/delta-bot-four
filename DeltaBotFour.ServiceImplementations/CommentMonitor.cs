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

        public async void Run()
        {
            // Get all new comments as they are posted
            // This will run as long as the application is running
            await Task.Factory.StartNew(() =>
            {
                var comments = _subreddit.Comments.GetListingStream();

                foreach (var comment in comments)
                {
                    // Get parent of this comment
                    var parent = _reddit.GetThingByFullname(comment.ParentId);

                    // If the parent isn't a comment, don't bother continuing
                    // It can't contain a valid delta
                    if(parent == null || !(parent is Comment))
                    {
                        continue;
                    }

                    // Convert to a DB4Comment and post comment to queue for processing
                    var db4Comment = new DB4Comment
                    {
                        Id = comment.Id,
                        ParentId = comment.ParentId,
                        LinkTitle = comment.LinkTitle,
                        AuthorName = comment.AuthorName,
                        ParentAuthorName = ((Comment)parent).AuthorName,
                        ShortLink = comment.Shortlink.Replace(LINK_REPLACE_FROM, LINK_REPLACE_TO),
                        Body = comment.Body
                    };

                    // Put on the queue for comment processing
                    _queue.Push(new QueueMessage(QueueMessageType.Comment, JsonConvert.SerializeObject(db4Comment)));
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
