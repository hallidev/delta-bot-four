using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpRedditService : IRedditService
    {
        private const string WwwRedditBaseUrl = "https://www.reddit.com";
        private const string OAuthRedditBaseUrl = "https://oauth.reddit.com";
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

        public DB4Thing GetCommentByUrl(string url)
        {
            if (url.StartsWith(WwwRedditBaseUrl))
            {
                url = url.Replace(WwwRedditBaseUrl, OAuthRedditBaseUrl);
            }

            var qualifiedComment = _reddit.GetCommentAsync(new Uri(url)).Result;
            return RedditThingConverter.Convert(qualifiedComment);
        }

        public void ReplyToComment(DB4Thing comment, string reply)
        {
            var qualifiedComment = getQualifiedComment(comment);
            Task.Run(async () =>
            {
                // All DB4 replies should be distinguished
                var newComment = await qualifiedComment.ReplyAsync(reply);
                await newComment.DistinguishAsync(ModeratableThing.DistinguishType.Moderator);
            }).Wait();
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

        public void ReplyToPrivateMessage(string privateMessageId, string body)
        {
            Task.Run(async () =>
            {
                if (_reddit.User == null)
                {
                    await _reddit.InitOrUpdateUserAsync();
                }

                // Get private message with the specified id
                var privateMessage = getPrivateMessageById(privateMessageId);
                await privateMessage.ReplyAsync(body);

            }).Wait();
        }

        public void SetPrivateMessageAsRead(string privateMessageId)
        {
            Task.Run(async () =>
            {
                if (_reddit.User == null)
                {
                    await _reddit.InitOrUpdateUserAsync();
                }

                // Get private message with the specified id
                var privateMessage = getPrivateMessageById(privateMessageId);
                await privateMessage.SetAsReadAsync();

            }).Wait();
        }

        private Comment getQualifiedComment(DB4Thing comment)
        {
            string commentUrl = $"{OAuthRedditBaseUrl}{comment.Shortlink}".TrimEnd('/');

            // Get comment with children and parent post populated
            return _reddit.GetCommentAsync(new Uri(commentUrl)).Result;
        }

        private PrivateMessage getPrivateMessageById(string privateMessageId)
        {
            return _reddit.User.GetInbox().Where(pm => pm.Id == privateMessageId && pm.Unread).FirstOrDefault().Result;
        }
    }
}
