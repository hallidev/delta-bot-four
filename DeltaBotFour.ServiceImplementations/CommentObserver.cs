using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;
using RedditSharp;
using System;
using System.Linq;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentObserver : IObserver<VotableThing>
    {
        private const string GET_COMMENT_URL_FROM = "www.reddit.com";
        private const string GET_COMMENT_URL_TO = "oauth.reddit.com";

        private Reddit _reddit;
        private Subreddit _subreddit;
        private IDB4Queue _queue;

        public CommentObserver(Reddit reddit, Subreddit subreddit, IDB4Queue queue)
        {
            _reddit = reddit;
            _subreddit = subreddit;
            _queue = queue;
        }

        public void OnNext(VotableThing votableThing)
        {
            Comment comment = votableThing as Comment;

            // We are only observing comments
            if (comment == null) { return; }

            var parentComment = _reddit.GetThingByFullnameAsync(comment.ParentId).Result;

            // Parent must be a comment to be considered by the
            // comment processor.
            if (parentComment is Comment)
            {
                CommentComposite commentComposite = new CommentComposite
                {
                    ParentComment = (Comment)parentComment,
                    Comment = comment
                };

                // If this is an edit, get children
                if(comment.Edited)
                {
                    Comment qualifiedComment = _reddit.GetCommentAsync(new Uri(comment.Shortlink.Replace(GET_COMMENT_URL_FROM, GET_COMMENT_URL_TO))).Result;
                    commentComposite.ChildComments = qualifiedComment.Comments.ToList();
                }

                _queue.Push(new QueueMessage(QueueMessageType.Comment, commentComposite));
            }
        }

        public void OnCompleted()
        {

        }

        public void OnError(Exception error)
        {

        }
    }
}
