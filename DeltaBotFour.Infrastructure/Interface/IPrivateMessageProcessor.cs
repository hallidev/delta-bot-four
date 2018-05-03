using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IPrivateMessageProcessor
    {
        void Process(DB4Thing privateMessage);
    }
}
