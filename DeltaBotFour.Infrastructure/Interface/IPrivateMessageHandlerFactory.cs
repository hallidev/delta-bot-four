using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IPrivateMessageHandlerFactory
    {
        IPrivateMessageHandler Create(DB4Thing privateMessage);
    }
}
