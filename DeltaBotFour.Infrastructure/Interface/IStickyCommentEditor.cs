using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IStickyCommentEditor
    {
        void UpsertOrRemoveSticky(DB4Thing post, int deltaCount);
    }
}
