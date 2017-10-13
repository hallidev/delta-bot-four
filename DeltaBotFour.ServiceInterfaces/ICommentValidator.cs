using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentValidator
    {
        DeltaCommentValidationResult Validate(DB4Comment comment);
    }
}
