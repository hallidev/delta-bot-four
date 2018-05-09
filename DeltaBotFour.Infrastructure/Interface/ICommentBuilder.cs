using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface ICommentBuilder
    {
        DB4Comment Build(DB4CommentType resultType, DB4Thing comment);
    }
}
