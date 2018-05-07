using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers
{
    public class StopQuotedDeltaWarningsPMHandler : IPrivateMessageHandler
    {
        private const string StopIndicator = "stop";

        private readonly AppConfiguration _appConfiguration;
        private readonly IDB4Repository _db4Repository;
        private readonly IRedditService _redditService;

        public StopQuotedDeltaWarningsPMHandler(AppConfiguration appConfiguration,
            IDB4Repository db4Repository, IRedditService redditService)
        {
            _appConfiguration = appConfiguration;
            _db4Repository = db4Repository;
            _redditService = redditService;
        }

        public void Handle(DB4Thing privateMessage)
        {
            // Check for a "stop" message
            if (privateMessage.Body.ToLower().Contains(StopIndicator))
            {
                // Ignore user
                _db4Repository.AddIgnoredQuotedDeltaPMUser(privateMessage.AuthorName);

                // Send confirmation message
                _redditService.ReplyToPrivateMessage(privateMessage.Id, _appConfiguration.PrivateMessages.ConfirmStopQuotedDeltaWarningMessage);
            }
        }
    }
}
