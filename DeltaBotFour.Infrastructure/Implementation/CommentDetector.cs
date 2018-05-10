using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class CommentDetector : ICommentDetector
    {
        private const string TOKEN_MATCH_REGEX = ".+";

        private readonly AppConfiguration _appConfiguration;
        private readonly List<Tuple<DB4CommentType, Regex>> _allComments = new List<Tuple<DB4CommentType, Regex>>();

        public CommentDetector(AppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;

            // Replace all possible replies with a regex for matching
            foreach (var db4Reply in _appConfiguration.Comments.AllComments)
            {
                var values = new Tuple<DB4CommentType, Regex>(db4Reply.Item2, new Regex(getPattern(db4Reply.Item1)));
                _allComments.Add(values);
            }
        }

        public DB4ReplyResult DidDB4MakeStickyComment(DB4Thing post)
        {
            var sticky = _allComments.First(c => c.Item1 == DB4CommentType.PostSticky);

            foreach (DB4Thing childComment in post.Comments)
            {
                if (childComment.AuthorName == _appConfiguration.DB4Username && sticky.Item2.IsMatch(childComment.Body))
                {
                    return new DB4ReplyResult
                    {
                        HasDB4Replied = true,
                        CommentType = DB4CommentType.PostSticky,
                        Comment = childComment
                    };
                }
            }

            // No sticky
            return new DB4ReplyResult
            {
                HasDB4Replied = false
            };
        }

        public DB4ReplyResult DidDB4Reply(DB4Thing comment)
        {
            // Check for a reply in the immediate children of the comment
            foreach(DB4Thing childComment in comment.Comments)
            {
                if (childComment.AuthorName == _appConfiguration.DB4Username)
                {
                    foreach (var reply in _allComments)
                    {
                        if (reply.Item2.IsMatch(childComment.Body))
                        {
                            // The sticky comment should never be detected in a comment reply
                            Assert.That(reply.Item1 != DB4CommentType.PostSticky);

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
            // Escape all tokens and special characters
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
                .Replace(")", "\\)");

            foreach (string token in _appConfiguration.ReplaceTokens.AllTokens)
            {
                pattern = pattern.Replace(token, TOKEN_MATCH_REGEX);
            }

            return $".*{pattern}.*";
        }
    }
}
