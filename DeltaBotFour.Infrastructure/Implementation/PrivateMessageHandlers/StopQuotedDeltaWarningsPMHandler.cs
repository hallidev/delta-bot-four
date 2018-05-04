using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;

namespace DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers
{
    public class StopQuotedDeltaWarningsPMHandler : IPrivateMessageHandler
    {
        private const string StopIndicator = "stop";

        private readonly AppConfiguration _appConfiguration;
        private readonly IDB4Repository _db4Repository;

        public StopQuotedDeltaWarningsPMHandler(AppConfiguration appConfiguration,
            IDB4Repository db4Repository)
        {
            _appConfiguration = appConfiguration;
            _db4Repository = db4Repository;
        }

        public void Handle(DB4Thing privateMessage)
        {
            // Handle PMs based on subject
            if (privateMessage.Subject == _appConfiguration.PrivateMessages.DeltaInQuoteSubject)
            {
                if (privateMessage.Body.ToLower().Contains(StopIndicator))
                {
                    _db4Repository.AddIgnoredQuotedDeltaPMUser(privateMessage.AuthorName);
                }
            }
        }
    }
}
