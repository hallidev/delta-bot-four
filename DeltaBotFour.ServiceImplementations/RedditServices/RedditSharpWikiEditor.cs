using System.Threading.Tasks;
using DeltaBotFour.ServiceInterfaces.RedditServices;
using RedditSharp.Things;

namespace DeltaBotFour.ServiceImplementations.RedditServices
{
    public class RedditSharpWikiEditor : IWikiEditor
    {
        private readonly Subreddit _subreddit;

        public RedditSharpWikiEditor(Subreddit subreddit)
        {
            _subreddit = subreddit;
        }

        public void EditPage(string url, string content)
        {
            Task.Run(async () => await _subreddit.GetWiki.EditPageAsync(url, content));
        }

        public string GetPage(string url)
        {
            var wiki = _subreddit.GetWiki;
            return wiki.GetPageAsync(url).Result.MarkdownContent;
        }
    }
}
