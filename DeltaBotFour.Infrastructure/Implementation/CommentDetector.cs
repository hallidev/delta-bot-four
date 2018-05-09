using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class CommentDetector : ICommentDetector
    {
        private const string TOKEN_MATCH_REGEX = ".+";

        private readonly AppConfiguration _appConfiguration;
        private readonly List<Tuple<DB4CommentType, Regex>> _allRepies = new List<Tuple<DB4CommentType, Regex>>();

        public CommentDetector(AppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;

            // Replace all possible replies with a regex for matching
            foreach (var db4Reply in _appConfiguration.Replies.AllReplies)
            {
                var values = new Tuple<DB4CommentType, Regex>(db4Reply.Item2, new Regex(getPattern(db4Reply.Item1)));
                _allRepies.Add(values);
            }
        }

        public DB4ReplyResult DidDB4Reply(DB4Thing comment)
        {
            // Check for a reply in the immediate children of the comment
            foreach(DB4Thing childComment in comment.Comments)
            {
                if (childComment.AuthorName == _appConfiguration.DB4Username)
                {
                    foreach (var reply in _allRepies)
                    {
                        if (reply.Item2.IsMatch(childComment.Body))
                        {
                            return new DB4ReplyResult
                            {
                                HasDB4Replied = true,
                                CommentType = reply.Item1,
                                Comment = childComment
                            };
                        }
                    }
                }
            }

            // DB4 hasn't replied yet
            return new DB4ReplyResult
            {
                HasDB4Replied = false
            };
        }

        private string getPattern(string db4reply)
        {
            // Escape tokens and special characters
            // []^$.|?*+()
            string pattern = db4reply
                .Replace("[", "\\[")
                .Replace("]", "\\]")
                .Replace("^", "\\^")
                .Replace("$", "\\$")
                .Replace(".", "\\.")
                .Replace("?", "\\?")
                .Replace("*", "\\*")
                .Replace("+", "\\+")
                .Replace("(", "\\(")
                .Replace(")", "\\)")
                .Replace(_appConfiguration.ReplaceTokens.ParentAuthorNameToken, TOKEN_MATCH_REGEX)
                .Replace(_appConfiguration.ReplaceTokens.DeltasToken, TOKEN_MATCH_REGEX)
                .Replace(_appConfiguration.ReplaceTokens.SubredditToken, TOKEN_MATCH_REGEX)
                .Replace(_appConfiguration.ReplaceTokens.IssueCountToken, TOKEN_MATCH_REGEX);

            return $".*{pattern}.*";
        }
    }
}
