using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface ICommentValidator
    {
        DeltaCommentValidationResult Validate(DB4Thing comment);
    }
}
