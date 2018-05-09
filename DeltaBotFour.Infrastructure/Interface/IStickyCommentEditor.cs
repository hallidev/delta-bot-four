using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IStickyCommentEditor
    {
        void UpsertSticky(DB4Thing post, int deltaCount);
    }
}
