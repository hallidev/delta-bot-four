using System;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class StickyCommentEditor : IStickyCommentEditor
    {
        private readonly ICommentBuilder _commentBuilder;
        private readonly ICommentDetector _commentDetector;
        private readonly ICommentReplier _commentReplier;
        private readonly IDB4Repository _repository;

        public StickyCommentEditor(ICommentBuilder commentBuilder,
            ICommentDetector commentDetector,
            ICommentReplier commentReplier,
            IDB4Repository repository)
        {
            _commentBuilder = commentBuilder;
            _commentDetector = commentDetector;
            _commentReplier = commentReplier;
            _repository = repository;
        }

        public void UpsertOrRemove(DB4Thing post, int? deltaCount, WATTArticle article, string deltaLogPostUrl)
        {
            // This method needs to do some extra work to handle WATT stuff
            // I'm not loving how this particular implementation came out...

            // Anyhow, we're expecting either a delta count from award / unaward OR a WATTArticle from a private message
            // Not both.
            if (deltaCount.HasValue && article != null)
            {
                throw new InvalidOperationException("Both a delta count and WATT article were provided when only one or the other was expected.");
            }

            // If we didn't get a delta count, look it up
            int finalDeltaCount;

            if (deltaCount.HasValue)
            {
                finalDeltaCount = deltaCount.Value;
            }
            else
            {
                finalDeltaCount = _repository.GetDeltaCommentsForPost(post.Id, post.AuthorName).Count;
            }

            // If we didn't get a WATT article, look it up
            // It's optional, so it could still be null after the lookup
            if (article == null)
            {
                article = _repository.GetWattArticleForPost(post.Id);
            }

            // Find out if the sticky comment has been made yet
            var result = _commentDetector.DidDB4MakeStickyComment(post);

            // During an Unaward, we can go down to zero.
            // If there are no deltas and no WATT article, delete the sticky
            if (deltaCount == 0 && article == null)
            {
                _commentReplier.DeleteReply(result.Comment);
                return;
            }

            // We'll need the updated sticky whether it has been made or not
            var db4Comment = _commentBuilder.BuildSticky(post, finalDeltaCount, article, deltaLogPostUrl);

            if (result.HasDB4Replied)
            {
                // Sticky exists, edit
                _commentReplier.EditReply(result.Comment, db4Comment);   
            }
            else
            {
                // No sticky, create it
                _commentReplier.Reply(post, db4Comment, true);
            }
        }
    }
}
