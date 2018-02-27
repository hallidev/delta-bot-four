using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface ICommentReplier
    {
        void Reply(DB4Thing comment, DeltaCommentValidationResult deltaCommentValidationResult);
        void EditReply(DB4Thing commentToEdit, DeltaCommentValidationResult deltaCommentValidationResult);
        void DeleteReply(DB4Thing commentToDelete);
    }
}
