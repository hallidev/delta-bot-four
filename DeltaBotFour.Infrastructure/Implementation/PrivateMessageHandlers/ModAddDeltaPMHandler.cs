using System;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers
{
    public class ModAddDeltaPMHandler : IPrivateMessageHandler
    {
        private const string AddFailedCantAwardDeltaBot = "You can't award DeltaBot a delta with the 'add' comment.";

        private const string AddFailedAuthorDeletedMessage =
            "The parent comment is deleted. DeltaBot can't add a delta.";

        private const string AddFailedAlreadyAwardedMessage =
            "I already successfully awarded a delta for this comment. I can't do 2 for the same comment.";

        private const string AddFailedErrorMessageFormat =
            "Add failed. DeltaBot is very sorry :(\n\nSend this to a DeltaBot dev:\n\n{0}";

        private const string AddSucceededMessage = "The 'add' command was processed successfully.";

        private readonly AppConfiguration _appConfiguration;
        private readonly IRedditService _redditService;
        private readonly ICommentDetector _commentDetector;
        private readonly ICommentBuilder _commentBuilder;
        private readonly ICommentReplier _replier;
        private readonly IDeltaAwarder _deltaAwarder;

        public ModAddDeltaPMHandler(AppConfiguration appConfiguration,
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
            var privateMessageParser = new PrivateMessageParser(privateMessage);
            var parseResult = privateMessageParser.Parse();

            var commentUrl = parseResult.Argument.Trim();

            try
            {
                // Get comment by url
                var comment = _redditService.GetCommentByUrl(commentUrl);

                // If that succeeded, we need the full comment with children to check for replies
                _redditService.PopulateParentAndChildren(comment);

                // Check for replies
                var db4ReplyResult = _commentDetector.DidDB4Reply(comment);

                // If the parent author is DeltaBot, bail
                if (comment.ParentThing.AuthorName == _appConfiguration.DB4Username)
                {
                    _redditService.ReplyToPrivateMessage(privateMessage.Id, AddFailedCantAwardDeltaBot);
                    return;
                }

                // If the parent author is [deleted], bail
                if (comment.ParentThing.AuthorName == Constants.DeletedAuthorName)
                {
                    _redditService.ReplyToPrivateMessage(privateMessage.Id, AddFailedAuthorDeletedMessage);
                    return;
                }

                // If a delta was already awarded successfully, bail
                if (db4ReplyResult.HasDB4Replied && db4ReplyResult.WasSuccessReply ||
                    db4ReplyResult.CommentType == DB4CommentType.ModeratorAdded)
                {
                    _redditService.ReplyToPrivateMessage(privateMessage.Id, AddFailedAlreadyAwardedMessage);
                    return;
                }

                // Build the normal success message
                // IMPORTANT: Build reply *before* awarding. This has to do with a quirk with getting the correct
                // delta count to build the reply. My bad...
                var reply = _commentBuilder.BuildReply(DB4CommentType.SuccessDeltaAwarded, comment);

                // Award delta
                _deltaAwarder.Award(comment);

                // Don't edit the existing comment - delete it and reply with the mod added reply
                // db4ReplyResult.Comment will be null if the mod is adding a delta directly to a comment
                if (db4ReplyResult.Comment != null)
                {
                    _replier.DeleteReply(db4ReplyResult.Comment);
                }

                _replier.Reply(comment, reply);

                // Build modmail body
                string body = _appConfiguration.PrivateMessages.ModAddedDeltaNotificationMessage
                    .Replace(_appConfiguration.ReplaceTokens.UsernameToken, privateMessage.AuthorName)
                    .Replace(_appConfiguration.ReplaceTokens.CommentLink, commentUrl);

                // Reply with modmail indicating success
                _redditService.SendPrivateMessage(_appConfiguration.PrivateMessages.ModAddedDeltaNotificationSubject,
                    body, $"/r/{_appConfiguration.SubredditName}");

                // Reply to user
                _redditService.ReplyToPrivateMessage(privateMessage.Id, AddSucceededMessage);
            }
            catch (Exception ex)
            {
                // Reply indicating failure
                _redditService.ReplyToPrivateMessage(privateMessage.Id,
                    string.Format(AddFailedErrorMessageFormat, ex.ToString()));

                // Rethrow for logging purposes
                throw;
            }
        }
    }
}