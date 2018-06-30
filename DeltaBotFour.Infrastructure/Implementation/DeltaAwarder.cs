using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;
using DeltaBotFour.Shared.Logging;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class DeltaAwarder : IDeltaAwarder
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly ILogger _logger;
        private readonly IUserWikiEditor _wikiEditor;
        private readonly IRedditService _redditService;
        private readonly ISubredditService _subredditService;
        private readonly IDeltaboardEditor _deltaboardEditor;
        private readonly IDeltaLogEditor _deltaLogEditor;
        private readonly IStickyCommentEditor _stickyCommentEditor;
        private readonly IDB4Repository _repository;

        public DeltaAwarder(AppConfiguration appConfiguration,
            ILogger logger,
            IUserWikiEditor wikiEditor,
            IRedditService redditService,
            ISubredditService subredditService, 
            IDeltaboardEditor deltaboardEditor,
            IDeltaLogEditor deltaLogEditor,
            IStickyCommentEditor stickyCommentEditor,
            IDB4Repository repository)
        {
            _appConfiguration = appConfiguration;
            _logger = logger;
            _wikiEditor = wikiEditor;
            _redditService = redditService;
            _subredditService = subredditService;
            _deltaboardEditor = deltaboardEditor;
            _deltaLogEditor = deltaLogEditor;
            _stickyCommentEditor = stickyCommentEditor;
            _repository = repository;
        }

        public void Award(DB4Thing comment)
        {
            // This can happen on "force add" command
            if (comment.ParentThing.AuthorName == Constants.DeletedAuthorName)
            {
                _logger.Info("---SKIPPING AWARD ON DELETED USER---");
                return;
            }

            _logger.Info($"---START AWARD DELTA--- -> user: {comment.ParentThing.AuthorName}, comment: {comment.Permalink}");

            // Safety check - if a delta has already been saved for this comment, it must be an edit
            if (_repository.DeltaCommentExists(comment.Id))
            {
                Assert.That(comment.IsEdited);
            }

            // Update wiki
            // The wiki is the standard from which delta counts come from
            _logger.Info("   ---Updating wiki (award)");
            int newDeltaCount = _wikiEditor.UpdateUserWikiEntryAward(comment);

            // If this was the user's first delta, send the first delta PM
            if (newDeltaCount == 1)
            {
                string subject = _appConfiguration.PrivateMessages.FirstDeltaSubject;
                string body = _appConfiguration.PrivateMessages.FirstDeltaMessage
                    .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName)
                    .Replace(_appConfiguration.ReplaceTokens.UsernameToken, comment.ParentThing.AuthorName);

                _logger.Info("   ---Sending first delta PM (award)");
                _redditService.SendPrivateMessage(subject, body, comment.ParentThing.AuthorName);
            }

            // Get new flair with incremented delta count
            string newFlairText = DeltaHelper.GetFlairText(newDeltaCount);

            // Award to the parent comment
            _logger.Info("   ---Setting flair (award)");
            _subredditService.SetUserFlair(comment.ParentThing.AuthorName, comment.ParentThing.AuthorFlairCssClass,
                newFlairText);

            // Update deltaboards
            _logger.Info("   ---Updating deltaboards (award)");
            _deltaboardEditor.AddDelta(comment.ParentThing.AuthorName);

            // After a successful award, record the DeltaComment
            var deltaComment = new DeltaComment
            {
                Id = comment.Id,
                ParentId = comment.ParentId,
                CreatedUtc = comment.CreatedUtc,
                IsEdited = comment.IsEdited,
                FromUsername = comment.AuthorName,
                ToUsername = comment.ParentThing.AuthorName,
                CommentText = comment.ParentThing.Body,
                LinkId = comment.LinkId,
                Permalink = comment.Permalink,
                Shortlink = comment.Shortlink,
                ParentPostId = comment.ParentPost.Id,
                ParentPostLinkId = comment.ParentPost.LinkId,
                ParentPostPermalink = comment.ParentPost.Permalink,
                ParentPostShortlink = comment.ParentPost.Shortlink,
                ParentPostTitle = comment.ParentPost.Title
            };

            // Upsert performs an insert or update depending on if it already exists
            _logger.Info("   ---Adding delta to local db (award)");
            _repository.UpsertDeltaComment(deltaComment);

            // Update DeltaLogs after repository update since it reads data from the repository
            _logger.Info("   ---Updating DeltaLog (award)");
            string deltaLogPostUrl = _deltaLogEditor.Upsert(comment.ParentPost.Id, comment.ParentPost.Permalink, comment.ParentPost.Title, comment.ParentPost.AuthorName);

            // Update sticky if this is OP
            // This needs to be absolute last since it relies on getting a Url from the DeltaLog post
            if (comment.AuthorName == comment.ParentPost.AuthorName)
            {
                // We need to get the count of deltas for this particular post
                var opDeltaCommentsInPost =
                    _repository.GetDeltaCommentsForPost(comment.ParentPost.Id, comment.ParentPost.AuthorName);

                // Update sticky comment
                _logger.Info("   ---Updating post sticky (award)");
                _stickyCommentEditor.UpsertOrRemove(comment.ParentPost, opDeltaCommentsInPost.Count, null, deltaLogPostUrl);
            }

            _logger.Info("---END AWARD DELTA---");
        }

        public void Unaward(DB4Thing comment)
        {
            // This can happen on "force add" command
            if (comment.ParentThing.AuthorName == Constants.DeletedAuthorName)
            {
                _logger.Info("---SKIPPING UNAWARD ON DELETED USER---");
                return;
            }

            _logger.Info($"---START UNAWARD DELTA--- -> user: {comment.ParentThing.AuthorName}, comment: {comment.Permalink}");

            // Safety check - if a delta hasnt' been saved for this comment, what are we trying to remove?
            if (!_repository.DeltaCommentExists(comment.Id))
            {
                Assert.That(comment.IsEdited);
            }

            // Update wiki
            // The wiki is the standard from which delta counts come from
            _logger.Info("   ---Updating wiki (unaward)");
            int newDeltaCount = _wikiEditor.UpdateUserWikiEntryUnaward(comment);

            string newFlairText = string.Empty;

            // If we are removing the user's only delta, we don't want the text to read "0∆"
            if (newDeltaCount != 0)
            {
                newFlairText = DeltaHelper.GetFlairText(newDeltaCount);
            }

            // Unaward from the parent comment
            _logger.Info("   ---Setting flair (unaward)");
            _subredditService.SetUserFlair(comment.ParentThing.AuthorName, comment.ParentThing.AuthorFlairCssClass,
                newFlairText);

            // Update deltaboards
            _logger.Info("   ---Updating deltaboards (unaward)");
            _deltaboardEditor.RemoveDelta(comment.ParentThing.AuthorName);

            // Remove from repository after successful unaward
            _logger.Info("   ---Removing delta from local db (unaward)");
            _repository.RemoveDeltaComment(comment.Id);

            // Update DeltaLogs after repository update since it reads data from the repository
            _logger.Info("   ---Updating DeltaLog (unaward)");
            string deltaLogPostUrl = _deltaLogEditor.Upsert(comment.ParentPost.Id, comment.ParentPost.Permalink, comment.ParentPost.Title, comment.ParentPost.AuthorName);

            // Update sticky if this is from OP
            if (comment.AuthorName == comment.ParentPost.AuthorName)
            {
                // We need to get the count of deltas for this particular post
                var opDeltaCommentsInPost =
                    _repository.GetDeltaCommentsForPost(comment.ParentPost.Id, comment.ParentPost.AuthorName);

                // Update or remove sticky comment - make sure to remove one from the count since we haven't removed the data from
                // the repository yet, so the current comment won't count
                _logger.Info("   ---Updating post sticky (unaward)");
                _stickyCommentEditor.UpsertOrRemove(comment.ParentPost, opDeltaCommentsInPost.Count, null, deltaLogPostUrl);
            }

            _logger.Info("---END UNAWARD DELTA---");
        }
    }
}
