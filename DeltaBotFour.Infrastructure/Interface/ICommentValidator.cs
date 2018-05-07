using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface ICommentValidator
    {
        DeltaCommentReply Validate(DB4Thing comment);
    }
}
