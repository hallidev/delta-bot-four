using Core.Foundation.Exceptions;
using Core.Foundation.Helpers;
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

        public DB4Comment BuildSticky(DB4Thing post, int deltaCount)
        {
            // Can only call BuildSticky on posts
            Assert.That(post.Type == DB4ThingType.Post);

            string body = _appConfiguration.Comments.PostSticky
                .Replace(_appConfiguration.ReplaceTokens.UsernameToken, post.AuthorName)
                .Replace(_appConfiguration.ReplaceTokens.CountToken, deltaCount.ToString())
                .Replace(_appConfiguration.ReplaceTokens.DeltaLogSubredditToken,
                    _appConfiguration.DeltaLogSubredditName);

            return new DB4Comment
            {
                CommentType = DB4CommentType.PostSticky,
                CommentBody = body
            };
        }

        public DB4Comment BuildReply(DB4CommentType commentType, DB4Thing comment)
        {
            // Can only call BuildReply on comments
            Assert.That(comment.Type == DB4ThingType.Comment);

            string body = string.Empty;

            switch (commentType)
            {
                case DB4CommentType.FailCommentTooShort:
                    body = _appConfiguration.Comments.CommentTooShort.Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken,
                        comment.ParentThing.AuthorName);
                    break;
                case DB4CommentType.FailCannotAwardOP:
                    body = _appConfiguration.Comments.CannotAwardOP;
                    break;
                case DB4CommentType.FailCannotAwardDeltaBot:
                    body = _appConfiguration.Comments.CannotAwardDeltaBot;
                    break;
                case DB4CommentType.FailCannotAwardSelf:
                    body = _appConfiguration.Comments.CannotAwardSelf;
                    break;
                case DB4CommentType.ModeratorAdded:
                    body = _appConfiguration.Comments.ModeratorAdded
                        .Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken, comment.ParentThing.AuthorName)
                        .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName)
                        .Replace(_appConfiguration.ReplaceTokens.DeltasToken, (DeltaHelper.GetDeltaCount(comment.ParentThing.AuthorFlairText) + 1).ToString());
                    break;
                case DB4CommentType.ModeratorRemoved:
                    body = _appConfiguration.Comments.ModeratorRemoved;
                    break;
                case DB4CommentType.SuccessDeltaAwarded:
                    body = _appConfiguration.Comments.DeltaAwarded
                        .Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken, comment.ParentThing.AuthorName)
                        .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName)
                        .Replace(_appConfiguration.ReplaceTokens.DeltasToken, (DeltaHelper.GetDeltaCount(comment.ParentThing.AuthorFlairText) + 1).ToString());
                    break;
                default:
                    throw new UnhandledEnumException<DB4CommentType>(commentType);
            }

            return new DB4Comment
            {
                CommentType = commentType,
                CommentBody = body
            };
        }
    }
}
