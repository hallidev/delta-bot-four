using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;
using RedditSharp;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpSubredditService : ISubredditService
    {
        private readonly RedditSharp.Reddit _reddit;
        private readonly Subreddit _subreddit;

        public RedditSharpSubredditService(RedditSharp.Reddit reddit, Subreddit subreddit)
        {
            _reddit = reddit;
            _subreddit = subreddit;
        }

        public DB4Thing Post(string title, string text, string subredditName = "")
        {
            var subReddit = _subreddit;

            // Since DB4 posts to another subreddit for DeltaLogs and registering multiple interfaces
            // against the DI container is a bit of a pain, default to main subreddit, but switch
            // to DeltaLog subreddit if passed in
            if (!string.IsNullOrEmpty(subredditName))
            {
                subReddit = _reddit.GetSubredditAsync($"/r/{subredditName}").Result;
            }

            // Submit post
            var post = subReddit.SubmitTextPostAsync(title, text).Result;

            // The returned post has basically nothing on it - need to retrieve a post with GetPost
            var oAuthUrl = UrlHelper.ConvertToOAuth(post.Url.AbsoluteUri);
            var fullPost = _reddit.GetPostAsync(new Uri(oAuthUrl)).Result;

            return RedditThingConverter.Convert(fullPost);
        }

        public bool IsUserModerator(string username)
        {
            var moderators = _subreddit.GetModeratorsAsync().Result;

            return moderators.Any(m => m.Name == username);
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
