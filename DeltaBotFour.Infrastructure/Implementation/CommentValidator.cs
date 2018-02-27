using System;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class CommentValidator : ICommentValidator
    {
        private readonly AppConfiguration _appConfiguration;

        public CommentValidator(AppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;
        }

        public DeltaCommentValidationResult Validate(DB4Thing comment)
        {
            // The immediate parent thing must be a comment (not a post) to be eligible
            if(comment.ParentThing.Type == DB4ThingType.Post)
            {
                return createValidationResult(DeltaCommentValidationResultType.FailCannotAwardOP, comment);
            }

            // Cannot award OP
            if (comment.ParentThing.AuthorName == comment.ParentPost.AuthorName)
            {
                return createValidationResult(DeltaCommentValidationResultType.FailCannotAwardOP, comment);
            }

            // Cannot award DeltaBot
            if (comment.ParentThing.AuthorName == _appConfiguration.DB4Username)
            {
                return createValidationResult(DeltaCommentValidationResultType.FailCannotAwardDeltaBot, comment);
            }

            // Cannot award self
            if (comment.ParentThing.AuthorName == comment.AuthorName)
            {
                return createValidationResult(DeltaCommentValidationResultType.FailCannotAwardSelf, comment);
            }

            // Check comment length
            // TODO: Uncomment
            //if (comment.Body.Length < _appConfiguration.ValidationValues.CommentTooShortLength)
            //{
            //    return createValidationResult(DeltaCommentValidationResultType.FailCommentTooShort, comment, parentThing);
            //}

            // TODO: Fail with issues

            // Success - valid delta
            return createValidationResult(DeltaCommentValidationResultType.SuccessDeltaAwarded, comment);
        }

        private DeltaCommentValidationResult createValidationResult(DeltaCommentValidationResultType resultType, DB4Thing comment)
        {
            string body = string.Empty;

            switch (resultType)
            {
                case DeltaCommentValidationResultType.FailCommentTooShort:
                    body = _appConfiguration.Replies.CommentTooShort.Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken,
                        comment.ParentThing.AuthorName);
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
                case DeltaCommentValidationResultType.SuccessDeltaAwarded:
                    body = _appConfiguration.Replies.DeltaAwarded
                        .Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken, comment.ParentThing.AuthorName)
                        .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName)
                        .Replace(_appConfiguration.ReplaceTokens.DeltasToken, DeltaHelper.GetDeltaCount(comment.ParentThing.AuthorFlairText).ToString() + 1);
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
