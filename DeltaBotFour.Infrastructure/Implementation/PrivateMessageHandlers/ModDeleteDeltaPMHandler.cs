using System;
using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers
{
    public class ModDeleteDeltaPMHandler : IPrivateMessageHandler
    {
        private const string DeleteFailedAuthorDeletedMessage =
            "The parent or parent's parent comment is deleted and a delta comment couldn't be found in the local database. DeltaBot can't delete.";

        private const string DeleteFailedNeverAwardedMessage =
            "I never awarded a delta for this comment, so there's nothing for me to delete!";

        private const string DeleteFailedErrorMessageFormat =
            "Delete failed. DeltaBot is very sorry :(\n\nSend this to a DeltaBot dev:\n\n{0}";

        private const string DeleteSucceededMessage = "The 'delete' command was processed successfully.";

        private readonly AppConfiguration _appConfiguration;
        private readonly IRedditService _redditService;
        private readonly ICommentDetector _commentDetector;
        private readonly ICommentBuilder _commentBuilder;
        private readonly ICommentReplier _replier;
        private readonly IDeltaAwarder _deltaAwarder;
        private readonly IDB4Repository _db4Repository;

        public ModDeleteDeltaPMHandler(AppConfiguration appConfiguration,
            IRedditService redditService,
            ICommentDetector commentDetector,
            ICommentBuilder commentBuilder,
            ICommentReplier replier,
            IDeltaAwarder deltaAwarder,
            IDB4Repository db4Repository)
        {
            _appConfiguration = appConfiguration;
            _redditService = redditService;
            _commentDetector = commentDetector;
            _commentBuilder = commentBuilder;
            _replier = replier;
            _deltaAwarder = deltaAwarder;
            _db4Repository = db4Repository;
        }

        public void Handle(DB4Thing privateMessage)
        {
            // The body should be the URL to DeltaBot's "Confirmed: 1 delta awarded..." comment
            string deltaBotAwardCommentUrl = privateMessage.Body.Trim();

            try
            {
                // Get comment by url
                var deltaBotAwardComment = _redditService.GetCommentByUrl(deltaBotAwardCommentUrl);

                // If that succeeded, we need a few more things:
                // 1: The parent comment with the !delta in it
                // 2: The parent's parent which is the comment that earned the delta
                _redditService.PopulateParentAndChildren(deltaBotAwardComment);

                Assert.That(deltaBotAwardComment.ParentThing.Type == DB4ThingType.Comment);

                // Populate the parent's parent - it should also be a comment
                _redditService.PopulateParentAndChildren(deltaBotAwardComment.ParentThing);

                Assert.That(deltaBotAwardComment.ParentThing.ParentThing.Type == DB4ThingType.Comment);

                // Check for replies
                var db4ReplyResult = _commentDetector.DidDB4Reply(deltaBotAwardComment.ParentThing);

                // If a delta was never awarded in the first place, bail
                if (!db4ReplyResult.HasDB4Replied || !db4ReplyResult.WasSuccessReply &&
                    db4ReplyResult.CommentType != DB4CommentType.ModeratorAdded)
                {
                    _redditService.ReplyToPrivateMessage(privateMessage.Id,
                        DeleteFailedNeverAwardedMessage);

                    return;
                }

                var isParentDeleted = deltaBotAwardComment.ParentThing.AuthorName == Constants.DeletedAuthorName;
                var isParentParentDeleted = deltaBotAwardComment.ParentThing.ParentThing.AuthorName ==
                                            Constants.DeletedAuthorName;

                // If either the parent or parent's parent is a deleted user, fallback
                // to database to try to find author information
                if (isParentDeleted || isParentParentDeleted)
                {
                    // Try to pull the author name from delta comments
                    var deltaComment =
                        _db4Repository.GetDeltaCommentByPermalink(deltaBotAwardComment.ParentThing.Permalink);

                    if (deltaComment == null)
                    {
                        // No delta comment found, can't proceed
                        _redditService.ReplyToPrivateMessage(privateMessage.Id, DeleteFailedAuthorDeletedMessage);
                        return;
                    }

                    // Set author info from database comment, this should allow unaward to proceed
                    deltaBotAwardComment.ParentThing.AuthorName = deltaComment.FromUsername;
                    deltaBotAwardComment.ParentThing.ParentThing.AuthorName = deltaComment.ToUsername;
                }

                // A delta was awarded, unaward it
                _deltaAwarder.Unaward(deltaBotAwardComment.ParentThing);

                // Build moderator removal message
                var reply = _commentBuilder.BuildReply(DB4CommentType.ModeratorRemoved,
                    deltaBotAwardComment.ParentThing);

                if (!isParentDeleted)
                {
                    // Don't edit the success comment - delete it and reply with the mod deleted reply
                    _replier.DeleteReply(db4ReplyResult.Comment);
                    _replier.Reply(deltaBotAwardComment.ParentThing, reply);
                }
                else
                {
                    // DeltaBot can't reply to deleted comments so we need to edit
                    _replier.EditReply(deltaBotAwardComment, reply);
                }

                // Build modmail body
                string body = _appConfiguration.PrivateMessages.ModDeletedDeltaNotificationMessage
                    .Replace(_appConfiguration.ReplaceTokens.UsernameToken, privateMessage.AuthorName)
                    .Replace(_appConfiguration.ReplaceTokens.CommentLink, deltaBotAwardComment.ParentThing.Permalink);

                // Reply with modmail indicating success
                _redditService.SendPrivateMessage(_appConfiguration.PrivateMessages.ModDeletedDeltaNotificationSubject,
                    body, $"/r/{_appConfiguration.SubredditName}");

                // Reply to user
                _redditService.ReplyToPrivateMessage(privateMessage.Id, DeleteSucceededMessage);
            }
            catch (Exception ex)
            {
                // Reply indicating failure
                _redditService.ReplyToPrivateMessage(privateMessage.Id,
                    string.Format(DeleteFailedErrorMessageFormat, ex));

                // Rethrow for logging purposes
                throw;
            }
        }
    }
}