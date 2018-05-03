using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;
using DeltaBotFour.Shared.Interface;
using Newtonsoft.Json;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpActivityDispatcher : IActivityDispatcher
    {
        private readonly IDB4Queue _queue;

        public RedditSharpActivityDispatcher(IDB4Queue queue)
        {
            _queue = queue;
        }

        public void SendToQueue(Comment comment)
        {
            var db4Thing = RedditThingConverter.Convert(comment);
            pushToQueue(db4Thing, QueueMessageType.Comment);
        }

        public void SendToQueue(PrivateMessage privateMessage)
        {
            var db4Thing = RedditThingConverter.Convert(privateMessage);
            pushToQueue(db4Thing, QueueMessageType.PrivateMessage);
        }

        private void pushToQueue(DB4Thing db4Thing, QueueMessageType messageType)
        {
            // Put on the queue for comment processing
            _queue.Push(new QueueMessage(messageType, JsonConvert.SerializeObject(db4Thing)));
        }
    }
}
