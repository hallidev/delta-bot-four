using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface ICommentBuilder
    {
        DB4Comment BuildSticky(DB4Thing post, int deltaCount, WATTArticle article);
        DB4Comment BuildReply(DB4CommentType commentType, DB4Thing comment);
    }
}
