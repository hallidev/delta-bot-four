using DeltaBotFour.Models;

namespace DeltaBotFour.Reddit.Interface
{
    public interface ISubredditService
    {
        DB4Thing Post(string title, string text, string subredditName = "");
        bool IsUserModerator(string username);
        void SetUserFlair(string username, string cssClass, string flairText);
        string GetWikiUrl();
        string GetWikiPage(string url);
        void EditPage(string url, string content);
        string GetSidebar();
        void UpdateSidebar(string sidebarContent);
    }
}
