using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class StickyCommentEditor : IStickyCommentEditor
    {
        private readonly ICommentBuilder _commentBuilder;
        private readonly ICommentDetector _commentDetector;
        private readonly ICommentReplier _commentReplier;

        public StickyCommentEditor(ICommentBuilder commentBuilder,
            ICommentDetector commentDetector,
            ICommentReplier commentReplier)
        {
            _commentBuilder = commentBuilder;
            _commentDetector = commentDetector;
            _commentReplier = commentReplier;
        }

        public void UpsertSticky(DB4Thing post, int deltaCount)
        {
            // Find out if the sticky comment has been made yet
            var result = _commentDetector.DidDB4MakeStickyComment(post);

            // During an Unaward, we can go down to zero
            // in that case, delete the comment and bail
            if (deltaCount == 0)
            {
                _commentReplier.DeleteReply(result.Comment);
                return;
            }

            // We'll need the updated sticky whether it has been made or not
            var db4Comment = _commentBuilder.BuildSticky(post, deltaCount);

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
