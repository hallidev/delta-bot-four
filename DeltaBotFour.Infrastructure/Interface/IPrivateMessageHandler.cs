using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IPrivateMessageHandler
    {
        void Handle(DB4Thing privateMessage);
    }
}
