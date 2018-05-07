using System;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class CommentReplyBuilder : ICommentReplyBuilder
    {
        private readonly AppConfiguration _appConfiguration;

        public CommentReplyBuilder(AppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;
        }

        public DeltaCommentReply Build(DeltaCommentReplyType resultType, DB4Thing comment)
        {
            string body = string.Empty;

            switch (resultType)
            {
                case DeltaCommentReplyType.FailCommentTooShort:
                    body = _appConfiguration.Replies.CommentTooShort.Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken,
                        comment.ParentThing.AuthorName);
                    break;
                case DeltaCommentReplyType.FailCannotAwardOP:
                    body = _appConfiguration.Replies.CannotAwardOP;
                    break;
                case DeltaCommentReplyType.FailCannotAwardDeltaBot:
                    body = _appConfiguration.Replies.CannotAwardDeltaBot;
                    break;
                case DeltaCommentReplyType.FailCannotAwardSelf:
                    body = _appConfiguration.Replies.CannotAwardSelf;
                    break;
                case DeltaCommentReplyType.FailModeratorRemoved:
                    body = _appConfiguration.Replies.ModeratorRemoved;
                    break;
                case DeltaCommentReplyType.SuccessDeltaAwarded:
                    body = _appConfiguration.Replies.DeltaAwarded
                        .Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken, comment.ParentThing.AuthorName)
                        .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName)
                        .Replace(_appConfiguration.ReplaceTokens.DeltasToken, (DeltaHelper.GetDeltaCount(comment.ParentThing.AuthorFlairText) + 1).ToString());
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled DeltaCommentValidationResult: {resultType}");
            }

            return new DeltaCommentReply
            {
                ResultType = resultType,
                ReplyCommentBody = body
            };
        }
    }
}
