using System;
using System.Net;
using System.Threading.Tasks;
using DeltaBotFour.Reddit.Interface;
using RedditSharp;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpSubredditService : ISubredditService
    {
        private readonly Subreddit _subreddit;

        public RedditSharpSubredditService(Subreddit subreddit)
        {
            _subreddit = subreddit;
        }

        public void SetUserFlair(string username, string cssClass, string flairText)
        {
            Task.Run(async () => await _subreddit.SetUserFlairAsync(username, cssClass, flairText)).Wait();
        }

        public string GetWikiUrl()
        {
            return $"{_subreddit.Url}wiki";
        }

        public string GetWikiPage(string url)
        {
            var wiki = _subreddit.GetWiki;

            try
            {
                return wiki.GetPageAsync(url).Result.MarkdownContent;
            }
            catch (Exception ex)
            {
                if (ex.InnerException is RedditHttpException httpException
                    && httpException.StatusCode == HttpStatusCode.NotFound)
                {
                    // Page wasn't found, just return null
                    return null;
                }

                // This was some other exception, make sure it's rethrown
                throw;
            }
        }

        public void EditPage(string url, string content)
        {
            Task.Run(async () => await _subreddit.GetWiki.EditPageAsync(url, content)).Wait();
        }

        public string GetSidebar()
        {
            var settings = _subreddit.GetSettingsAsync().Result;

            return settings.Sidebar;
        }

        public void UpdateSidebar(string sidebarContent)
        {
            var settings = _subreddit.GetSettingsAsync().Result;
            settings.Sidebar = sidebarContent;

            Task.Run(async () => await settings.UpdateSettings()).Wait();
        }
    }
}
