using Core.Foundation.IoC;
using DeltaBotFour.ServiceImplementations;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp;

namespace DeltaBotFour.DependencyResolver
{
    public class DeltaBotFourRegistrationCatalog : IRegistrationCatalog
    {
        public void Register(IModularContainer container)
        {
            var appConfiguration = new AppConfiguration();

            var botWebAgent = new BotWebAgent
            (
                username: appConfiguration.DB4Username,
                password: appConfiguration.DB4Password,
                clientID: appConfiguration.DB4ClientId,
                clientSecret: appConfiguration.DB4ClientSecret,
                redirectURI: "http://localhost"
            );

            var reddit = new Reddit(botWebAgent, false);
            var subreddit = reddit.GetSubreddit($"/r/{appConfiguration.SubredditName}");

            // Register core / shared classes
            container.RegisterSingleton(appConfiguration);
            container.RegisterSingleton(botWebAgent);
            container.RegisterSingleton(reddit);
            container.RegisterSingleton(subreddit);

            // Register functionality implementations
            container.Register<ICommentMonitor, CommentMonitor>();
        }
    }
}
