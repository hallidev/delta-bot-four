using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpPrivateMessageService : IPrivateMessageService
    {
        private const string RedditBaseUrl = "https://oauth.reddit.com";
        private readonly RedditSharp.Reddit _reddit;

        public RedditSharpPrivateMessageService(RedditSharp.Reddit reddit)
        {
            _reddit = reddit;
        }

        public void SetAsRead(string fullName, string id)
        {
            var privateMessage = _reddit.GetThingByFullnameAsync(id).Result;
            //_reddit.gett
        }
    }
}
