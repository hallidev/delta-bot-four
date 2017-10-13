using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using Newtonsoft.Json;
using RedditSharp;
using RedditSharp.Things;
using System;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentObserver : IObserver<VotableThing>
    {
        private string SHORT_LINK_FROM = "www.reddit.com";
        private string SHORT_LINK_TO = "oauth.reddit.com";
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
            if (comment == null) { return; }

            var db4Comment = getDB4Comment(comment);

            if (db4Comment != null)
            {
                pushCommentToQueue(db4Comment);
            }
        }

        private DB4Comment getDB4Comment(Comment comment)
        {
            // Convert to a DB4Comment
            return new DB4Comment
            {
                ParentId = comment.ParentId,
                ShortLink = comment.Shortlink.Replace(SHORT_LINK_FROM, SHORT_LINK_TO),
                Body = comment.Body,
                IsEdited = comment.Edited
            };
        }

        private void pushCommentToQueue(DB4Comment db4Comment)
        {
            // Put on the queue for comment processing
            _queue.Push(new QueueMessage(QueueMessageType.Comment, JsonConvert.SerializeObject(db4Comment)));
        }
    }
}
