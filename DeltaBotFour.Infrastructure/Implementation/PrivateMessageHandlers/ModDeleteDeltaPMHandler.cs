using System;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers
{
    public class ModDeleteDeltaPMHandler : IPrivateMessageHandler
    {
        private const string DeleteFailedNeverAwardedMessage = "I never awarded a delta for this comment, so there's nothing for me to delete!";
        private const string DeleteFailedErrorMessageFormat = "Delete failed. DeltaBot is very sorry :(\n\nSend this to a DeltaBot dev:\n\n{0}";

        private readonly AppConfiguration _appConfiguration;
        private readonly IRedditService _redditService;
        private readonly ICommentDetector _commentDetector;
        private readonly ICommentBuilder _commentBuilder;
        private readonly ICommentReplier _replier;
        private readonly IDeltaAwarder _deltaAwarder;

        public ModDeleteDeltaPMHandler(AppConfiguration appConfiguration,
            IRedditService redditService,
            ICommentDetector commentDetector, 
            ICommentBuilder commentBuilder,
            ICommentReplier replier,
            IDeltaAwarder deltaAwarder)
        {
            _appConfiguration = appConfiguration;
            _redditService = redditService;
            _commentDetector = commentDetector;
            _commentBuilder = commentBuilder;
            _replier = replier;
            _deltaAwarder = deltaAwarder;
        }

        public void Handle(DB4Thing privateMessage)
        {
            // The body should be the URL to a comment
            string commentUrl = privateMessage.Body.Trim();

            try
            {
                // Get comment by url
                var comment = _redditService.GetCommentByUrl(commentUrl);

                // If that succeeded, we need the full comment with children to check for replies
                _redditService.PopulateParentAndChildren(comment);

                // Check for replies
                var db4ReplyResult = _commentDetector.DidDB4Reply(comment);

                // If a delta was never awarded in the first place, bail
                if (!db4ReplyResult.HasDB4Replied || !db4ReplyResult.WasSuccessReply && db4ReplyResult.CommentType != DB4CommentType.ModeratorAdded)
                {
                    _redditService.ReplyToPrivateMessage(privateMessage.Id,
                        DeleteFailedNeverAwardedMessage);

                    return;
                }

                // A delta was awarded, unaward it
                _deltaAwarder.Unaward(comment);

                // Build moderator removal message
                var reply = _commentBuilder.BuildReply(DB4CommentType.ModeratorRemoved, comment);

                // Don't edit the success comment - delete it and reply with the mod deleted reply
                _replier.DeleteReply(db4ReplyResult.Comment);
                _replier.Reply(comment, reply);

                string body = _appConfiguration.PrivateMessages.ModDeletedDeltaNotificationMessage
                    .Replace(_appConfiguration.ReplaceTokens.CommentLink, commentUrl);

                // Reply with modmail indicating success
                _redditService.SendPrivateMessage(_appConfiguration.PrivateMessages.ModDeletedDeltaNotificationSubject,
                    body, $"/r/{_appConfiguration.SubredditName}");

            }
            catch (Exception ex)
            {
                // Reply indicating failure
                _redditService.ReplyToPrivateMessage(privateMessage.Id, 
                    string.Format(DeleteFailedErrorMessageFormat, ex.ToString()));

                // Rethrow for logging purposes
                throw;
            }
        }
    }
}
