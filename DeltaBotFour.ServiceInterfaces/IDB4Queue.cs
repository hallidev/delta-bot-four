using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface IDB4Queue
    {
        void Push(QueueMessage message);
        QueueMessage Pop();
    }
}
