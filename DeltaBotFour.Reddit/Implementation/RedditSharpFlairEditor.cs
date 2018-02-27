using System.Threading.Tasks;
using DeltaBotFour.Reddit.Interface;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpFlairEditor : IFlairEditor
    {
        private readonly Subreddit _subreddit;

        public RedditSharpFlairEditor(Subreddit subreddit)
        {
            _subreddit = subreddit;
        }

        public void SetUserFlair(string username, string cssClass, string flairText)
        {
            Task.Run(async () => await _subreddit.SetUserFlairAsync(username, cssClass, flairText));
        }
    }
}
