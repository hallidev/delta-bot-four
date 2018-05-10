using System;
using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class DeltaAwarder : IDeltaAwarder
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IUserWikiEditor _wikiEditor;
        private readonly IRedditService _redditService;
        private readonly ISubredditService _subredditService;
        private readonly IDeltaboardEditor _deltaboardEditor;
        private readonly IDeltaLogEditor _deltaLogEditor;
        private readonly IStickyCommentEditor _stickyCommentEditor;
        private readonly IDB4Repository _repository;

        public DeltaAwarder(AppConfiguration appConfiguration,
            IUserWikiEditor wikiEditor,
            IRedditService redditService,
            ISubredditService subredditService, 
            IDeltaboardEditor deltaboardEditor,
            IDeltaLogEditor deltaLogEditor,
            IStickyCommentEditor stickyCommentEditor,
            IDB4Repository repository)
        {
            _appConfiguration = appConfiguration;
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
            // Safety check - if a delta has already been saved for this comment, it must be an edit
            // TODO: Re-enable check after development is complete
            //if (_repository.DeltaCommentExists(comment.Id))
            //{
            //    Assert.That(comment.IsEdited);
            //}

            // Get the user's current delta count from flair
            int currentDeltaCount = DeltaHelper.GetDeltaCount(comment.ParentThing.AuthorFlairText);

            // Get new flair with incremented delta count
            string newFlairText = DeltaHelper.GetIncrementedFlairText(comment.ParentThing.AuthorFlairText);

            // Award to the parent comment
            _subredditService.SetUserFlair(comment.ParentThing.AuthorName, comment.ParentThing.AuthorFlairCssClass,
                newFlairText);

            // Update wiki
            _wikiEditor.UpdateUserWikiEntryAward(comment);

            // If this was the user's first delta, send the first delta PM
            if (currentDeltaCount == 0)
            {
                string subject = _appConfiguration.PrivateMessages.FirstDeltaSubject;
                string body = _appConfiguration.PrivateMessages.FirstDeltaMessage
                    .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName)
                    .Replace(_appConfiguration.ReplaceTokens.UsernameToken, comment.ParentThing.AuthorName);

                _redditService.SendPrivateMessage(subject, body, comment.ParentThing.AuthorName);
            }

            // Update deltaboards
            _deltaboardEditor.AddDelta(comment.ParentThing.AuthorName);

            // After a successful award, record the DeltaComment
            var deltaComment = new DeltaComment
            {
                Id = comment.Id,
                ParentId = comment.ParentId,
                CreatedUTC = comment.CreatedUTC,
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
            _repository.UpsertDeltaComment(deltaComment);

            // Update DeltaLogs after repository update since it reads data from the repository
            string deltaLogPostUrl = _deltaLogEditor.Upsert(comment.ParentPost.Id, comment.ParentPost.Permalink);

            // Update sticky if this is OP
            // This needs to be absolute last since it relies on getting a Url from the DeltaLog post
            if (comment.AuthorName == comment.ParentPost.AuthorName)
            {
                // We need to get the count of deltas for this particular post
                var opDeltaCommentsInPost =
                    _repository.GetDeltaCommentsForPost(comment.ParentPost.Id, comment.ParentPost.AuthorName);

                // Update sticky comment
                _stickyCommentEditor.UpsertOrRemove(comment.ParentPost, opDeltaCommentsInPost.Count, null, deltaLogPostUrl);
            }

            ConsoleHelper.WriteLine($"DeltaBot awarded a delta -> user: {comment.ParentThing.AuthorName}", ConsoleColor.Green);
        }

        public void Unaward(DB4Thing comment)
        {
            // Safety check - if a delta hasnt' been saved for this comment, what are we trying to remove?
            if (!_repository.DeltaCommentExists(comment.Id))
            {
                Assert.That(comment.IsEdited);
            }

            // Get the user's current delta count from flair
            int currentDeltaCount = DeltaHelper.GetDeltaCount(comment.ParentThing.AuthorFlairText);

            string newFlairText = string.Empty;

            // If we are removing the user's only delta, we don't want the text to read "0∆"
            if (currentDeltaCount != 1)
            {
                newFlairText = DeltaHelper.GetDecrementedFlairText(comment.ParentThing.AuthorFlairText);
            }

            // Unaward from the parent comment
            _subredditService.SetUserFlair(comment.ParentThing.AuthorName, comment.ParentThing.AuthorFlairCssClass,
                newFlairText);

            // Update wiki
            _wikiEditor.UpdateUserWikiEntryUnaward(comment);

            // Update deltaboards
            _deltaboardEditor.RemoveDelta(comment.ParentThing.AuthorName);

            // Remove from repository after successful unaward
            _repository.RemoveDeltaComment(comment.Id);

            // Update DeltaLogs after repository update since it reads data from the repository
            string deltaLogPostUrl = _deltaLogEditor.Upsert(comment.ParentPost.Id, comment.ParentPost.Permalink);

            // Update sticky if this is from OP
            if (comment.AuthorName == comment.ParentPost.AuthorName)
            {
                // We need to get the count of deltas for this particular post
                var opDeltaCommentsInPost =
                    _repository.GetDeltaCommentsForPost(comment.ParentPost.Id, comment.ParentPost.AuthorName);

                // Update or remove sticky comment - make sure to remove one from the count since we haven't removed the data from
                // the repository yet, so the current comment won't count
                _stickyCommentEditor.UpsertOrRemove(comment.ParentPost, opDeltaCommentsInPost.Count, null, deltaLogPostUrl);
            }

            ConsoleHelper.WriteLine($"DeltaBot unawarded a delta -> user: {comment.ParentThing.AuthorName}", ConsoleColor.Green);
        }
    }
}
