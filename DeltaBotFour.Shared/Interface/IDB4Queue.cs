using DeltaBotFour.Models;

namespace DeltaBotFour.Shared.Interface
{
    public interface IDB4Queue
    {
        void Push(QueueMessage message);
        QueueMessage Pop();
    }
}
