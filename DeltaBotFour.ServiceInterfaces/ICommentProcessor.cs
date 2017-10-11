using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentProcessor
    {
        void Process(DB4Comment comment);
    }
}
