using System;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers
{
    public class ModDeleteDeltaPMHandler : IPrivateMessageHandler
    {
        private const string DeleteSucceededMessage = "Delta has been deleted.";
        private const string DeleteFailedNeverAwardedMessage = "I never awarded a delta for this comment, so there's nothing for me to delete!";
        private const string DeleteFailedErrorMessageFormat = "Delete failed. DeltaBot is very sorry :(\n\nSend this to a DeltaBot dev:\n\n{0}";

        private readonly IRedditService _redditService;
        private readonly ICommentReplyDetector _replyDetector;

        public ModDeleteDeltaPMHandler(IRedditService redditService,
            ICommentReplyDetector replyDetector)
        {
            _redditService = redditService;
            _replyDetector = replyDetector;
        }

        public void Handle(DB4Thing privateMessage)
        {
            // The body should be the URL to a comment
            string url = privateMessage.Body.Trim();

            try
            {
                // Get comment by url
                var comment = _redditService.GetCommentByUrl(url);

                // If that succeeded, we need the full comment with children to check for replies
                _redditService.PopulateParentAndChildren(comment);

                // Check for replies
                var result = _replyDetector.DidDB4Reply(comment);

                // If a delta was never awarded in the first place, bail
                if (!result.HasDB4Replied || !result.WasSuccessReply)
                {
                    _redditService.ReplyToPrivateMessage(privateMessage.Id,
                        DeleteFailedNeverAwardedMessage);

                    return;
                }

                // A delta was awarded, unaward it


                // Reply indicating success
                _redditService.ReplyToPrivateMessage(privateMessage.Id, 
                    DeleteSucceededMessage);

            }
            catch (Exception ex)
            {
                // Reply indicating failure
                _redditService.ReplyToPrivateMessage(privateMessage.Id, 
                    string.Format(DeleteFailedErrorMessageFormat, ex.ToString()));

                // Rethrow for logging purposes
                throw;
            }

            // Make sure DeltaBot had actually awarded a delta

        }
    }
}
