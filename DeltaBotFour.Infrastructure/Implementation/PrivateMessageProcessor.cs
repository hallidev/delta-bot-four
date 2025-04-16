using System;
using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;
using DeltaBotFour.Shared.Logging;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class PrivateMessageProcessor : IPrivateMessageProcessor
    {
        private readonly ILogger _logger;
        private readonly IPrivateMessageHandlerFactory _privateMessageHandlerFactory;
        private readonly IRedditService _redditService;

        public PrivateMessageProcessor(ILogger logger,
            IPrivateMessageHandlerFactory privateMessageHandlerFactory,
            IRedditService redditService)
        {
            _logger = logger;
            _privateMessageHandlerFactory = privateMessageHandlerFactory;
            _redditService = redditService;
        }

        public void Process(DB4Thing privateMessage)
        {
            // If we got here with a comment or post, that's a problem
            Assert.That(privateMessage.Type == DB4ThingType.PrivateMessage,
                $"CommentProcessor received type: {privateMessage.Type}");

            _logger.Info(
                $"Processing incoming private message from: {privateMessage.AuthorName}, subject: {privateMessage.Subject}");

            // Get a handler to handle this private message
            var handler = _privateMessageHandlerFactory.Create(privateMessage);

            // The handler could be null if this kind of private message doesn't have handler
            if (handler != null)
            {
                _logger.Info($"PrivateMessageHandler created: {handler.GetType()}");

                try
                {
                    // Try to handle the message
                    handler.Handle(privateMessage);
                }
                catch (Exception)
                {
                    // Remember to set message to read even if there was an exception
                    _redditService.SetPrivateMessageAsRead(privateMessage.Id);

                    // Re-throw - it will be handled and logged by dispatcher
                    throw;
                }
            }

            // After handling the private message, set it to read
            _redditService.SetPrivateMessageAsRead(privateMessage.Id);

            _logger.Info("Done processing private message.");
        }
    }
}