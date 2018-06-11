using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Persistence.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class CommentValidator : ICommentValidator
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IDB4Repository _db4Repository;
        private readonly ICommentBuilder _commentBuilder;

        public CommentValidator(AppConfiguration appConfiguration,
            IDB4Repository db4Repository,
            ICommentBuilder commentBuilder)
        {
            _appConfiguration = appConfiguration;
            _db4Repository = db4Repository;
            _commentBuilder = commentBuilder;
        }

        public DB4Comment Validate(DB4Thing comment)
        {
            // The immediate parent thing must be a comment (not a post) to be eligible
            if(comment.ParentThing.Type == DB4ThingType.Post)
            {
                return _commentBuilder.BuildReply(DB4CommentType.FailCannotAwardOP, comment);
            }

            // Cannot award self
            if (comment.ParentThing.AuthorName == comment.AuthorName)
            {
                return _commentBuilder.BuildReply(DB4CommentType.FailCannotAwardSelf, comment);
            }

            // Cannot award OP
            if (comment.ParentThing.AuthorName == comment.ParentPost.AuthorName)
            {
                return _commentBuilder.BuildReply(DB4CommentType.FailCannotAwardOP, comment);
            }

            // Cannot award twice for same comment
            if (_db4Repository.DeltaCommentExistsForParentCommentByAuthor(comment.ParentId, comment.AuthorName))
            {
                return _commentBuilder.BuildReply(DB4CommentType.FailCannotAwardTwice, comment);
            }

            // Cannot award DeltaBot
            if (comment.ParentThing.AuthorName == _appConfiguration.DB4Username)
            {
                return _commentBuilder.BuildReply(DB4CommentType.FailCannotAwardDeltaBot, comment);
            }

            // Check comment length
            if (comment.Body.Length < _appConfiguration.ValidationValues.CommentTooShortLength)
            {
                return _commentBuilder.BuildReply(DB4CommentType.FailCommentTooShort, comment);
            }

            // Success - valid delta
            return _commentBuilder.BuildReply(DB4CommentType.SuccessDeltaAwarded, comment);
        }
    }
}
