using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IUserWikiEditor
    {
        int GetCurrentDeltaCount(string userName);
        int UpdateUserWikiEntryAward(DB4Thing comment);
        int UpdateUserWikiEntryUnaward(DB4Thing comment);
    }
}
