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
            // With RedditSharp, comment.Parent is the parent Post, not parent comment
            // I'm using the "parentThing" variable for the parent comment
            Post parentPost = (Post)comment.Parent;

            string opName = parentPost.AuthorName;

            // The parent must be a comment (not a post) to be eligible
            if(parentThing is Post)
            {
                return createValidationResult(DeltaCommentValidationResultType.FailCannotAwardOP, comment, parentThing);
            }

            // Check comment length
            if(comment.Body.Length < _appConfiguration.ValidationValues.CommentTooShortLength)
            {
                return createValidationResult(DeltaCommentValidationResultType.FailCommentTooShort, comment, parentThing);
            }

            Comment parentComment = (Comment)parentThing;

            // Cannot award OP
            if (parentComment.AuthorName == opName)
            {
                return createValidationResult(DeltaCommentValidationResultType.FailCannotAwardOP, comment, parentThing);
            }

            // Cannot award DeltaBot
            if (parentComment.AuthorName == _appConfiguration.DB4Username)
            {
                return createValidationResult(DeltaCommentValidationResultType.FailCannotAwardDeltaBot, comment, parentThing);
            }

            // Cannot award self
            if (parentComment.AuthorName == comment.AuthorName)
            {
                return createValidationResult(DeltaCommentValidationResultType.FailCannotAwardSelf, comment, parentThing);
            }

            // TODO: Fail with issues
            // TODO: Rejected

            // Success - valid delta
            return createValidationResult(DeltaCommentValidationResultType.SuccessDeltaAwarded, comment, parentThing);
        }

        private DeltaCommentValidationResult createValidationResult(DeltaCommentValidationResultType resultType, Comment comment, Thing parentThing)
        {
            string body = string.Empty;

            Comment parentComment = parentThing as Comment;

            switch (resultType)
            {
                case DeltaCommentValidationResultType.FailCommentTooShort:
                    body = _appConfiguration.Replies.CommentTooShort.Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken,
                        parentComment.AuthorName);
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
                        .Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken, parentComment.AuthorName)
                        .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName);
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
