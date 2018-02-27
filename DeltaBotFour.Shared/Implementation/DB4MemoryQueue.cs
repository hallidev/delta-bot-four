using System.Collections;
using DeltaBotFour.Models;
using DeltaBotFour.Shared.Interface;
using Newtonsoft.Json;

namespace DeltaBotFour.Shared.Implementation
{
    public class DB4MemoryQueue : IDB4Queue
    {
        private readonly Queue _queue = new Queue();

        public void Push(QueueMessage message)
        {
            _queue.Enqueue(JsonConvert.SerializeObject(message));
        }

        public QueueMessage Pop()
        {
            if (_queue.Count > 0)
            {
                return JsonConvert.DeserializeObject<QueueMessage>(_queue.Dequeue().ToString());
            }

            return null;
        }
    }
}
