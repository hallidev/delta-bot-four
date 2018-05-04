using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class PrivateMessageProcessor : IPrivateMessageProcessor
    {
        private const string StopIndicator = "stop";

        private readonly AppConfiguration _appConfiguration;
        private readonly IDB4Repository _db4Repository;

        public PrivateMessageProcessor(AppConfiguration appConfiguration,
            IDB4Repository db4Repository)
        {
            _appConfiguration = appConfiguration;
            _db4Repository = db4Repository;
        }

        public void Process(DB4Thing privateMessage)
        {
            // If we got here with a comment or post, that's a problem
            Assert.That(privateMessage.Type == DB4ThingType.PrivateMessage, $"CommentProcessor received type: {privateMessage.Type}");

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
