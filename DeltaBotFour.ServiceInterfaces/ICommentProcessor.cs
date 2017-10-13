using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentProcessor
    {
        void Process(CommentComposite commentComposite);
    }
}
