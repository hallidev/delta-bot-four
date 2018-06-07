namespace DeltaBotFour.Reddit
{
    internal static class UrlHelper
    {
        public const string WwwRedditBaseUrl = "https://www.reddit.com";
        public const string OAuthRedditBaseUrl = "https://oauth.reddit.com";

        public static string ConvertToOAuth(string url)
        {
            // Convert www to oauth
            if (url.StartsWith(WwwRedditBaseUrl))
            {
                return url.Replace(WwwRedditBaseUrl, OAuthRedditBaseUrl);
            }

            // If we still aren't starting with oauth, change the relative to absolute url
            if (!url.StartsWith(OAuthRedditBaseUrl))
            {
                url = $"{OAuthRedditBaseUrl}{url}";
            }

            return url;
        }

        public static string BuildSubredditApiUrl(string subredditName, string apiPath)
        {
            return $"{OAuthRedditBaseUrl}/r/{subredditName}{apiPath}";
        }
    }
}
