using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class PrivateMessageProcessor : IPrivateMessageProcessor
    {
        private readonly IPrivateMessageHandlerFactory _privateMessageHandlerFactory;
        private readonly IPrivateMessageService _privateMessageService;

        public PrivateMessageProcessor(IPrivateMessageHandlerFactory privateMessageHandlerFactory, 
            IPrivateMessageService privateMessageService)
        {
            _privateMessageHandlerFactory = privateMessageHandlerFactory;
            _privateMessageService = privateMessageService;
        }

        public void Process(DB4Thing privateMessage)
        {
            // If we got here with a comment or post, that's a problem
            Assert.That(privateMessage.Type == DB4ThingType.PrivateMessage, $"CommentProcessor received type: {privateMessage.Type}");

            // Get a handler to handle this private message
            var handler = _privateMessageHandlerFactory.Create(privateMessage);

            // The handler could be null if this kind of private message doesn't have handler
            handler?.Handle(privateMessage);

            // After handling the private message, set it to read
            _privateMessageService.SetAsRead(privateMessage.FullName, privateMessage.Id);
        }
    }
}
