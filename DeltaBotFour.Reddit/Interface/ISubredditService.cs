﻿namespace DeltaBotFour.Reddit.Interface
{
    public interface ISubredditService
    {
        void SetUserFlair(string username, string cssClass, string flairText);
        string GetWikiUrl();
        string GetWikiPage(string url);
        void EditPage(string url, string content);
        string GetSidebar();
        void UpdateSidebar(string sidebarContent);
    }
}
