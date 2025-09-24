using System;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers
{
    public class AddDeltaPMHandler : IPrivateMessageHandler
    {
        private const string AddFailedErrorMessageFormat =
            "Add failed. DeltaBot is very sorry :(\n\nSend this to a DeltaBot dev:\n\n{0}";

        private const string AddSucceededMessage =
            "The 'add' command was processed successfully. The comment is being rescanned and if it was eligible for a delta, a delta will be added.";

        private readonly ICommentProcessor _commentProcessor;
        private readonly IRedditService _redditService;

        public AddDeltaPMHandler(ICommentProcessor commentProcessor,
            IRedditService redditService)
        {
            _commentProcessor = commentProcessor;
            _redditService = redditService;
        }

        public void Handle(DB4Thing privateMessage)
        {
            // The body should be the URL to a comment
            var privateMessageParser = new PrivateMessageParser(privateMessage);
            var parseResult = privateMessageParser.Parse();

            var commentUrl = parseResult.Argument.Trim();

            try
            {
                // Get comment by url
                var comment = _redditService.GetCommentByUrl(commentUrl);

                // Process the comment for delta award / unaward
                _commentProcessor.Process(comment);

                // Reply to user
                _redditService.ReplyToPrivateMessage(privateMessage.Id, AddSucceededMessage);
            }
            catch (Exception ex)
            {
                // Reply indicating failure
                _redditService.ReplyToPrivateMessage(privateMessage.Id,
                    string.Format(AddFailedErrorMessageFormat, ex));

                // Rethrow for logging purposes
                throw;
            }
        }
    }
}