using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IUserWikiEditor
    {
        void UpdateUserWikiEntryAward(DB4Thing comment);
        void UpdateUserWikiEntryUnaward(DB4Thing comment);
    }
}
