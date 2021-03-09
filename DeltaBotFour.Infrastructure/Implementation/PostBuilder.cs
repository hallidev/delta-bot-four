using System.Collections.Generic;
using System.Linq;
using Core.Foundation.Extensions;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class PostBuilder : IPostBuilder
    {
        private const int MaxChars = 100;

        private readonly AppConfiguration _appConfiguration;

        public PostBuilder(AppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;
        }

        public (string, string) BuildDeltaLogPost(string mainPostTitle, string mainPostPermalink, string opUsername, List<DeltaComment> deltaComments)
        {
            string title = _appConfiguration.Posts.DeltaLogTitle
                .Replace(_appConfiguration.ReplaceTokens.PostTitle, mainPostTitle);

            var opDeltaComments = deltaComments.Where(c => c.FromUsername == opUsername).ToList();

            // Start the rows as either "None Yet." or empty based on if there are any
            string opRowContent = opDeltaComments.Count == 0 ? _appConfiguration.Posts.DeltaNoRowContent : string.Empty;

            foreach (var deltaComment in opDeltaComments)
            {
                opRowContent += _appConfiguration.Posts.DeltaOPRowContent
                    .Replace(_appConfiguration.ReplaceTokens.UsernameToken, deltaComment.ToUsername)
                    .Replace(_appConfiguration.ReplaceTokens.CommentLink, $"{deltaComment.Permalink}?context=3")
                    .Replace(_appConfiguration.ReplaceTokens.CommentText, Sanitize(deltaComment.CommentText).Ellipsis(MaxChars));
                opRowContent += "\n\n";
            }

            var otherDeltaComments = deltaComments.Where(c => c.FromUsername != opUsername).ToList();

            // Start the rows as either "None Yet." or empty based on if there are any
            string otherRowContent = otherDeltaComments.Count == 0 ? _appConfiguration.Posts.DeltaNoRowContent : string.Empty;

            foreach (var deltaComment in otherDeltaComments)
            {
                otherRowContent += _appConfiguration.Posts.DeltaOtherRowContent
                    .Replace(_appConfiguration.ReplaceTokens.UsernameFromToken, deltaComment.FromUsername)
                    .Replace(_appConfiguration.ReplaceTokens.UsernameToken, deltaComment.ToUsername)
                    .Replace(_appConfiguration.ReplaceTokens.CommentLink, $"{deltaComment.Permalink}?context=3")
                    .Replace(_appConfiguration.ReplaceTokens.CommentText, Sanitize(deltaComment.CommentText).Ellipsis(MaxChars));
                otherRowContent += "\n\n";
            }

            string content = _appConfiguration.Posts.DeltaLogContent
                .Replace(_appConfiguration.ReplaceTokens.PostLink, mainPostPermalink)
                .Replace(_appConfiguration.ReplaceTokens.UsernameToken, opUsername)
                .Replace(_appConfiguration.ReplaceTokens.DeltaLogOPRowsToken, opRowContent)
                .Replace(_appConfiguration.ReplaceTokens.DeltaLogOtherRowsToken, otherRowContent);

            return (title, content);
        }

        public static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\n", string.Empty);
        }
    }
}
