using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentProcessor
    {
        void Process(QueueMessage<DB4Comment> comment);
    }
}
