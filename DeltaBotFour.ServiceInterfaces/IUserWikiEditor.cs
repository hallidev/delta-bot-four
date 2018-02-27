using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface IUserWikiEditor
    {
        void UpdateUserWikiEntryAward(DB4Thing comment, DB4Thing parentComment);
        void UpdateUserWikiEntryUnaward(DB4Thing comment, DB4Thing parentComment);
    }
}
