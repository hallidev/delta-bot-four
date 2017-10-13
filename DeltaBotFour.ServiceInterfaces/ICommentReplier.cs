using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentReplier
    {
        void Reply(CommentComposite comment, DeltaCommentValidationResult deltaCommentValidationResult);
    }
}
