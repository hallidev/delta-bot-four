using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentValidator
    {
        DeltaCommentValidationResult Validate(CommentComposite commentComposite);
    }
}
