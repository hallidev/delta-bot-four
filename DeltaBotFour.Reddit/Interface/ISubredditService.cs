namespace DeltaBotFour.Reddit.Interface
{
    public interface ISubredditService
    {
        void SetUserFlair(string username, string cssClass, string flairText);
        string GetPage(string url);
        void EditPage(string url, string content);
        string GetWikiUrl();
        string GetSidebar();
        void UpdateSidebar(string sidebarContent);
    }
}
