using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using Newtonsoft.Json;
using System.Collections;

namespace DeltaBotFour.ServiceImplementations
{
    public class DB4MemoryQueue : IDB4Queue
    {
        private Queue _queue = new Queue();

        public DB4MemoryQueue()
        {

        }

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
