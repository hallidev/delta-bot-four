using System.Threading.Tasks;
using DeltaBotFour.Reddit.Interface;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpWikiEditor : IWikiEditor
    {
        private readonly Subreddit _subreddit;

        public RedditSharpWikiEditor(Subreddit subreddit)
        {
            _subreddit = subreddit;
        }

        public string GetWikiUrl()
        {
            return $"{_subreddit.Url}wiki";
        }

        public void EditPage(string url, string content)
        {
            Task.Run(async () => await _subreddit.GetWiki.EditPageAsync(url, content)).Wait();
        }

        public string GetPage(string url)
        {
            var wiki = _subreddit.GetWiki;
            return wiki.GetPageAsync(url).Result.MarkdownContent;
        }
    }
}
