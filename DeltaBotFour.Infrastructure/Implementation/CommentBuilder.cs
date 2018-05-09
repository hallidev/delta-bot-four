using Core.Foundation.Exceptions;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class CommentBuilder : ICommentBuilder
    {
        private readonly AppConfiguration _appConfiguration;

        public CommentBuilder(AppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;
        }

        public DB4Comment Build(DB4CommentType resultType, DB4Thing comment)
        {
            string body = string.Empty;

            switch (resultType)
            {
                case DB4CommentType.FailCommentTooShort:
                    body = _appConfiguration.Replies.CommentTooShort.Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken,
                        comment.ParentThing.AuthorName);
                    break;
                case DB4CommentType.FailCannotAwardOP:
                    body = _appConfiguration.Replies.CannotAwardOP;
                    break;
                case DB4CommentType.FailCannotAwardDeltaBot:
                    body = _appConfiguration.Replies.CannotAwardDeltaBot;
                    break;
                case DB4CommentType.FailCannotAwardSelf:
                    body = _appConfiguration.Replies.CannotAwardSelf;
                    break;
                case DB4CommentType.ModeratorAdded:
                    body = _appConfiguration.Replies.ModeratorAdded
                        .Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken, comment.ParentThing.AuthorName)
                        .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName)
                        .Replace(_appConfiguration.ReplaceTokens.DeltasToken, (DeltaHelper.GetDeltaCount(comment.ParentThing.AuthorFlairText) + 1).ToString());
                    break;
                case DB4CommentType.ModeratorRemoved:
                    body = _appConfiguration.Replies.ModeratorRemoved;
                    break;
                case DB4CommentType.SuccessDeltaAwarded:
                    body = _appConfiguration.Replies.DeltaAwarded
                        .Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken, comment.ParentThing.AuthorName)
                        .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName)
                        .Replace(_appConfiguration.ReplaceTokens.DeltasToken, (DeltaHelper.GetDeltaCount(comment.ParentThing.AuthorFlairText) + 1).ToString());
                    break;
                default:
                    throw new UnhandledEnumException<DB4CommentType>(resultType);
            }

            return new DB4Comment
            {
                CommentType = resultType,
                CommentBody = body
            };
        }
    }
}
