using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class PrivateMessageProcessor : IPrivateMessageProcessor
    {
        private readonly IPrivateMessageHandlerFactory _privateMessageHandlerFactory;

        public PrivateMessageProcessor(IPrivateMessageHandlerFactory privateMessageHandlerFactory)
        {
            _privateMessageHandlerFactory = privateMessageHandlerFactory;
        }

        public void Process(DB4Thing privateMessage)
        {
            // If we got here with a comment or post, that's a problem
            Assert.That(privateMessage.Type == DB4ThingType.PrivateMessage, $"CommentProcessor received type: {privateMessage.Type}");

            // Get a handler to handle this private message
            var handler = _privateMessageHandlerFactory.Create(privateMessage);

            // The handler could be null if this kind of private message doesn't have handler
            handler?.Handle(privateMessage);
        }
    }
}
