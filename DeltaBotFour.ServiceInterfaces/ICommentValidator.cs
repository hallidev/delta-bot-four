using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentValidator
    {
        DeltaCommentValidationResult Validate(DB4Thing comment, DB4Thing parentThing);
    }
}
