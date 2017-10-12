using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentReplyDetector
    {
        bool DidDB4Reply(DB4Comment comment);
    }
}
