using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface ICommentReplyDetector
    {
        DB4ReplyResult DidDB4Reply(DB4Thing comment);
    }
}
