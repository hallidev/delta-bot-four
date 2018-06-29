using DeltaBotFour.Models;

namespace DeltaBotFour.Shared.Interface
{
    public interface IDB4Queue
    {
        int GetPrimaryCount();
        int GetNinjaEditCount();
        void Push(QueueMessage message);
        QueueMessage Pop();
    }
}
