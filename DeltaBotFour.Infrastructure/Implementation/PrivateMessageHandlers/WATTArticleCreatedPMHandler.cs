using System;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation.PrivateMessageHandlers
{
    public class WATTArticleCreatedPMHandler : IPrivateMessageHandler
    {
        private const string CreateFailedInvalidFormatMessage =
            "WATT article creation failed. Expected 4 lines with ID on line 1, Fullname on line 2, title on line 3, URL on line 4.";

        private readonly IDB4Repository _db4Repository;
        private readonly IRedditService _redditService;
        private readonly IStickyCommentEditor _stickyCommentEditor;

        public WATTArticleCreatedPMHandler(IDB4Repository db4Repository,
            IRedditService redditService,
            IStickyCommentEditor stickyCommentEditor)
        {
            _db4Repository = db4Repository;
            _redditService = redditService;
            _stickyCommentEditor = stickyCommentEditor;
        }

        public void Handle(DB4Thing privateMessage)
        {
            // First split the PM up on newlines
            var privateMessageLines = privateMessage.Body.Split(
                new[] {"\r\n", "\r", "\n"},
                StringSplitOptions.RemoveEmptyEntries);

            // We are expecting two lines - first with a title and second with a URL
            // ex:
            // 8hr3tt
            // t3_8hr3tt
            // This is a WATT article
            // https://www.url.com
            if (privateMessageLines.Length != 4)
            {
                _redditService.ReplyToPrivateMessage(privateMessage.Id, CreateFailedInvalidFormatMessage);
                return;
            }

            // Create article
            string postId = privateMessageLines[0];
            string postFullname = privateMessageLines[1];
            string title = privateMessageLines[2];
            string url = privateMessageLines[3];

            var article = new WATTArticle
            {
                Id = Guid.NewGuid(),
                RedditPostId = postId,
                Title = title,
                Url = url
            };

            // Save article
            _db4Repository.UpsertWATTArticle(article);

            // Need to retrieve the full post to make life easy
            var post = _redditService.GetThingByFullname(postFullname);
            _redditService.PopulateChildren(post);

            // Update sticky for the post in question
            _stickyCommentEditor.UpsertOrRemoveSticky(post, null, article);
        }
    }
}
