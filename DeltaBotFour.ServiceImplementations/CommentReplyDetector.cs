using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;
using RedditSharp.Things;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentReplyDetector : ICommentReplyDetector
    {
        private const string TOKEN_MATCH_REGEX = ".+";

        private readonly AppConfiguration _appConfiguration;
        private readonly List<Regex> _successReplyRegexes = new List<Regex>();
        private readonly List<Regex> _failReplyRegexes = new List<Regex>();

        public CommentReplyDetector(AppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;

            // Replace all possible replies with a regex for matching
            foreach (string db4Reply in _appConfiguration.Replies.SuccessReplies)
            {
                _successReplyRegexes.Add(new Regex(getPattern(db4Reply)));
            }

            foreach (string db4Reply in _appConfiguration.Replies.FailReplies)
            {
                _failReplyRegexes.Add(new Regex(getPattern(db4Reply)));
            }
        }

        public DB4ReplyResult DidDB4Reply(Comment comment)
        {
            // Check for a reply in the immediate children of the comment
            foreach(Comment childComment in comment.Comments)
            {
                if (childComment.AuthorName == _appConfiguration.DB4Username)
                {
                    foreach (Regex regex in _successReplyRegexes)
                    {
                        if (regex.IsMatch(childComment.Body))
                        {
                            return new DB4ReplyResult { HasDB4Replied = true, WasSuccessReply = true, Comment = childComment };
                        }
                    }

                    foreach (Regex regex in _failReplyRegexes)
                    {
                        if (regex.IsMatch(childComment.Body))
                        {
                            return new DB4ReplyResult { HasDB4Replied = true, WasSuccessReply = false, Comment = childComment };
                        }
                    }
                }
            }

            return new DB4ReplyResult { HasDB4Replied = false, WasSuccessReply = false };
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
