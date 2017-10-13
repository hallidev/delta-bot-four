using DeltaBotFour.ServiceInterfaces;
using System;
using DeltaBotFour.Models;
using RedditSharp.Things;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentValidator : ICommentValidator
    {
        private AppConfiguration _appConfiguration;

        public CommentValidator(AppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;
        }

        public DeltaCommentValidationResult Validate(Comment comment, Thing parentThing)
        {
            // The parent must be a comment (not a post) to be eligible
            if(comment.Parent is Post)
            {
                return createValidationResult(DeltaCommentValidationResultType.FailCannotAwardOP);
            }

            // Cannot award yourself a delta
            return null;
        }

        private DeltaCommentValidationResult createValidationResult(DeltaCommentValidationResultType resultType)
        {
            string body = string.Empty;

            switch (resultType)
            {
                case DeltaCommentValidationResultType.FailCommentTooShort:
                    body = _appConfiguration.Replies.CommentTooShort;
                    break;
                case DeltaCommentValidationResultType.FailCannotAwardOP:
                    body = _appConfiguration.Replies.CannotAwardOP;
                    break;
                case DeltaCommentValidationResultType.FailCannotAwardDeltaBot:
                    body = _appConfiguration.Replies.CannotAwardDeltaBot;
                    break;
                case DeltaCommentValidationResultType.FailCannotAwardSelf:
                    body = _appConfiguration.Replies.CannotAwardSelf;
                    break;
                case DeltaCommentValidationResultType.FailRejected:
                    body = _appConfiguration.Replies.Rejected;
                    break;
                case DeltaCommentValidationResultType.SuccessDeltaAwarded:
                    body = _appConfiguration.Replies.DeltaAwarded;
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled DeltaCommentValidationResult: {resultType}");
            }

            return new DeltaCommentValidationResult
            {
                ResultType = resultType,
                ReplyCommentBody = body
            };
        }
    }
}
