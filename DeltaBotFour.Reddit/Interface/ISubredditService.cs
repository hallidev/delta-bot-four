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
        void EditWikiPage(string url, string content);
        string GetSidebar();
        string GetSidebarWidgetId(string sidebarWidgetName);
        void UpdateSidebar(string sidebarContent);
        void UpdateSidebarWidget(string sidebarWidgetName, string sidebarContent);
    }
}
