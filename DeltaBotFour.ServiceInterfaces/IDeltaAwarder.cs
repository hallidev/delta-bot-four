using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface IDeltaAwarder
    {
        void Award(DB4Comment comment);
    }
}
