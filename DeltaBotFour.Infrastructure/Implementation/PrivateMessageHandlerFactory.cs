using DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class PrivateMessageHandlerFactory : IPrivateMessageHandlerFactory
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IDB4Repository _db4Repository;
        private readonly IRedditService _redditService;
        private readonly ISubredditService _subredditService;

        public PrivateMessageHandlerFactory(AppConfiguration appConfiguration,
            IDB4Repository db4Repository, 
            IRedditService redditService, 
            ISubredditService subredditService)
        {
            _appConfiguration = appConfiguration;
            _db4Repository = db4Repository;
            _redditService = redditService;
            _subredditService = subredditService;
        }

        public IPrivateMessageHandler Create(DB4Thing privateMessage)
        {
            // Add Delta (moderator only)
            if (_subredditService.IsUserModerator(privateMessage.AuthorName) &&
                privateMessage.Subject.ToLower().Contains(_appConfiguration.PrivateMessages.ModAddDeltaSubject.ToLower()))
            {
                return new ModAddDeltaPMHandler();
            }

            // Remove delta (moderator only)
            if (_subredditService.IsUserModerator(privateMessage.AuthorName) &&
                privateMessage.Subject.ToLower().Contains(_appConfiguration.PrivateMessages.ModDeleteDeltaSubject.ToLower()))
            {
                return new ModDeleteDeltaPMHandler();
            }

            // Stop quoted deltas warning
            if (privateMessage.Subject.ToLower()
                .Contains(_appConfiguration.PrivateMessages.DeltaInQuoteSubject.ToLower()))
            {
                return new StopQuotedDeltaWarningsPMHandler(_appConfiguration, _db4Repository, _redditService);
            }

            return null;
        }
    }
}
