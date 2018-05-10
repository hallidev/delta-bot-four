using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IStickyCommentEditor
    {
        void UpsertOrRemove(DB4Thing post, int? deltaCount, WATTArticle article);
    }
}
