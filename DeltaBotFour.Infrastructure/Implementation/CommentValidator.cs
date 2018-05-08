using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class CommentValidator : ICommentValidator
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly ICommentReplyBuilder _replyBuilder;

        public CommentValidator(AppConfiguration appConfiguration, ICommentReplyBuilder replyBuilder)
        {
            _appConfiguration = appConfiguration;
            _replyBuilder = replyBuilder;
        }

        public DeltaCommentReply Validate(DB4Thing comment)
        {
            // The immediate parent thing must be a comment (not a post) to be eligible
            if(comment.ParentThing.Type == DB4ThingType.Post)
            {
                return _replyBuilder.Build(DeltaCommentReplyType.FailCannotAwardOP, comment);
            }

            // Cannot award OP
            if (comment.ParentThing.AuthorName == comment.ParentPost.AuthorName)
            {
                return _replyBuilder.Build(DeltaCommentReplyType.FailCannotAwardOP, comment);
            }

            // Cannot award DeltaBot
            if (comment.ParentThing.AuthorName == _appConfiguration.DB4Username)
            {
                return _replyBuilder.Build(DeltaCommentReplyType.FailCannotAwardDeltaBot, comment);
            }

            // Cannot award self
            if (comment.ParentThing.AuthorName == comment.AuthorName)
            {
                return _replyBuilder.Build(DeltaCommentReplyType.FailCannotAwardSelf, comment);
            }

            // Check comment length
            if (comment.Body.Length < _appConfiguration.ValidationValues.CommentTooShortLength)
            {
                return _replyBuilder.Build(DeltaCommentReplyType.FailCommentTooShort, comment);
            }

            // Success - valid delta
            return _replyBuilder.Build(DeltaCommentReplyType.SuccessDeltaAwarded, comment);
        }
    }
}
