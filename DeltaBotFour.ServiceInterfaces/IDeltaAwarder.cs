using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface IDeltaAwarder
    {
        void Award(CommentComposite comment);
    }
}
