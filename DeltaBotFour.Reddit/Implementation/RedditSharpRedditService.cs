using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpRedditService : IRedditService
    {
        private const string RedditBaseUrl = "https://oauth.reddit.com";
        private readonly RedditSharp.Reddit _reddit;

        public RedditSharpRedditService(RedditSharp.Reddit reddit)
        {
            _reddit = reddit;
        }

        public void PopulateParentAndChildren(DB4Thing comment)
        {
            // Get comment with children and parent post populated
            var qualifiedComment = getQualifiedComment(comment);

            // Set parent post
            comment.ParentPost = RedditThingConverter.Convert(qualifiedComment.Parent);

            // Convert immediate children only
            var childComments = new List<DB4Thing>();

            foreach (Comment childComment in qualifiedComment.Comments)
            {
                childComments.Add(RedditThingConverter.Convert(childComment));
            }

            comment.Comments = childComments;

            // Get the parent thing - this could be the same as ParentPost above or it could be a comment
            var parentThing = _reddit.GetThingByFullnameAsync(comment.ParentId).Result;
            comment.ParentThing = RedditThingConverter.Convert(parentThing);
        }

        public DB4Thing GetThingByFullname(string fullname)
        {
            var unqualifiedComment = _reddit.GetThingByFullnameAsync(fullname).Result;
            return RedditThingConverter.Convert(unqualifiedComment);
        }

        public void ReplyToComment(DB4Thing comment, string reply)
        {
            var qualifiedComment = getQualifiedComment(comment);
            Task.Run(async () => await qualifiedComment.ReplyAsync(reply)).Wait();
        }

        public void EditComment(DB4Thing comment, string editedComment)
        {
            var qualifiedComment = getQualifiedComment(comment);
            Task.Run(async () => await qualifiedComment.EditTextAsync(editedComment)).Wait();
        }

        public void DeleteComment(DB4Thing comment)
        {
            var qualifiedComment = getQualifiedComment(comment);
            Task.Run(async () => await qualifiedComment.DelAsync()).Wait();
        }

        public void SendPrivateMessage(string subject, string body, string to, string fromSubreddit = "")
        {
            // This seems to be a RedditSharp quirk? I looked at the RedditSharp
            // codebase and it appears that you need to do this
            Task.Run(async () =>
            {
                if (_reddit.User == null)
                {
                    await _reddit.InitOrUpdateUserAsync();
                }

                await _reddit.ComposePrivateMessageAsync(subject, body, to, fromSubreddit);
            }).Wait();
        }

        private Comment getQualifiedComment(DB4Thing comment)
        {
            string commentUrl = $"{RedditBaseUrl}{comment.Shortlink}".TrimEnd('/');

            // Get comment with children and parent post populated
            return _reddit.GetCommentAsync(new Uri(commentUrl)).Result;
        }
    }
}
