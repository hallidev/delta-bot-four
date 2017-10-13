using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
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
            _queue.Enqueue(message);
        }

        public QueueMessage Pop()
        {
            if (_queue.Count > 0)
            {
                return (QueueMessage)_queue.Dequeue();
            }

            return null;
        }
    }
}
