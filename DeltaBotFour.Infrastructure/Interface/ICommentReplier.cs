using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface ICommentReplier
    {
        void Reply(DB4Thing comment, DeltaCommentReply deltaCommentReply);
        void EditReply(DB4Thing commentToEdit, DeltaCommentReply deltaCommentReply);
        void DeleteReply(DB4Thing commentToDelete);
    }
}
