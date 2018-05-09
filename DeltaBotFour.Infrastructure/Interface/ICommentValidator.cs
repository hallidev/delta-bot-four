using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface ICommentValidator
    {
        DB4Comment Validate(DB4Thing comment);
    }
}
