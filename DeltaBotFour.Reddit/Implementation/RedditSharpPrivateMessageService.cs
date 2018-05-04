using System.Linq;
using System.Threading.Tasks;
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

        public void SetAsRead(string id)
        {
            Task.Run(async () =>
            {
                if (_reddit.User == null)
                {
                    await _reddit.InitOrUpdateUserAsync();
                }

                // Get private message with the specified id
                await _reddit.User.GetInbox().Where(pm => pm.Id == id && pm.Unread)
                    .ForEachAsync(async pm =>
                    {
                        // Set as read
                        await pm.SetAsReadAsync();
                    });

            }).Wait();
        }
    }
}
