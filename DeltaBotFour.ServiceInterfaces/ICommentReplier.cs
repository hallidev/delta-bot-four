using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentReplier
    {
        void Reply(DB4Comment comment, DeltaCommentValidationResult deltaCommentValidationResult);
    }
}
