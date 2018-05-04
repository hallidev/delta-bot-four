using DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class PrivateMessageHandlerFactory : IPrivateMessageHandlerFactory
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IDB4Repository _db4Repository;

        public PrivateMessageHandlerFactory(AppConfiguration appConfiguration,
            IDB4Repository db4Repository)
        {
            _appConfiguration = appConfiguration;
            _db4Repository = db4Repository;
        }

        public IPrivateMessageHandler Create(DB4Thing privateMessage)
        {
            // Handle PMs based on subject
            if (privateMessage.Subject.ToLower().Contains(_appConfiguration.PrivateMessages.DeltaInQuoteSubject.ToLower()))
            {
                return new StopQuotedDeltaWarningsPMHandler(_db4Repository);
            }

            return null;
        }
    }
}
