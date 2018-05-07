using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface ICommentReplyBuilder
    {
        DeltaCommentReply Build(DeltaCommentReplyType resultType, DB4Thing comment);
    }
}
