using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;
using DeltaBotFour.Shared.Interface;
using Newtonsoft.Json;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpCommentDispatcher : ICommentDispatcher
    {
        private readonly IDB4Queue _queue;

        public RedditSharpCommentDispatcher(IDB4Queue queue)
        {
            _queue = queue;
        }

        public void SendToQueue(Comment comment)
        {
            var db4Comment = RedditThingConverter.Convert(comment);
            pushCommentToQueue(db4Comment);
        }

        private void pushCommentToQueue(DB4Thing db4Comment)
        {
            // Put on the queue for comment processing
            _queue.Push(new QueueMessage(QueueMessageType.Comment, JsonConvert.SerializeObject(db4Comment)));
        }
    }
}
