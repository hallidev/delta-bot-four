using Core.Foundation.IoC;
using DeltaBotFour.Infrastructure;
using DeltaBotFour.Infrastructure.Implementation;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Persistence.Implementation;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Implementation;
using DeltaBotFour.Reddit.Interface;
using DeltaBotFour.Shared.Implementation;
using DeltaBotFour.Shared.Interface;
using RedditSharp;

namespace DeltaBotFour.DependencyResolver
{
    public class DeltaBotFourRegistrationCatalog : IRegistrationCatalog
    {
        public void Register(IModularContainer container)
        {
            var appConfiguration = new AppConfiguration();

            // Actual login is performed here.
            // TODO: This should be abstracted away as well
            var botWebAgent = new BotWebAgent
            (
                appConfiguration.DB4Username,
                appConfiguration.DB4Password,
                appConfiguration.DB4ClientId,
                appConfiguration.DB4ClientSecret,
                 "http://localhost"
            );

            var reddit = new RedditSharp.Reddit(botWebAgent, false);
            var subreddit = reddit.GetSubredditAsync($"/r/{appConfiguration.SubredditName}").Result;

            // Register core / shared classes
            container.RegisterSingleton(appConfiguration);
            container.RegisterSingleton(botWebAgent);
            container.RegisterSingleton(reddit);
            container.RegisterSingleton(subreddit);

            // Register shared services
            container.Register<IDB4Queue, DB4MemoryQueue>();

            // Register persistence services
            container.Register<IDB4Repository, DB4Repository>();

            // Register Reddit Services
            container.Register<IActivityDispatcher, RedditSharpActivityDispatcher>();
            container.Register<IActivityMonitor, RedditSharpActivityMonitor>();
            container.Register<IRedditService, RedditSharpRedditService>();
            container.Register<ISubredditService, RedditSharpSubredditService>();

            // Register functionality implementations
            container.Register<IDB4QueueDispatcher, DB4QueueDispatcher>();
            container.Register<ICommentProcessor, CommentProcessor>();
            container.Register<ICommentBuilder, CommentBuilder>();
            container.Register<ICommentDetector, CommentDetector>();
            container.Register<ICommentValidator, CommentValidator>();
            container.Register<ICommentReplier, CommentReplier>();
            container.Register<IDeltaAwarder, DeltaAwarder>();
            container.Register<IPrivateMessageProcessor, PrivateMessageProcessor>();
            container.Register<IPrivateMessageHandlerFactory, PrivateMessageHandlerFactory>();
            container.Register<IUserWikiEditor, UserWikiEditor>();
            container.Register<IDeltaboardEditor, DeltaboardEditor>();
        }
    }
}
