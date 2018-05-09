using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface ICommentReplier
    {
        void Reply(DB4Thing thing, DB4Comment db4Comment, bool isSticky = false);
        void EditReply(DB4Thing commentToEdit, DB4Comment db4Comment);
        void DeleteReply(DB4Thing commentToDelete);
    }
}
