using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Foundation.Exceptions;
using Core.Foundation.Helpers;
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
            // We're always calling these on processed comments
            Assert.That(comment.Type == DB4ThingType.Comment);

            // Get comment with children and parent post populated
            var qualifiedComment = (Comment) getQualifiedThing(comment);

            // Set parent post
            comment.ParentPost = RedditThingConverter.Convert(qualifiedComment.Parent);

            // We also want all of the immediate children (comments) of a Post
            if (qualifiedComment.Parent is Post parentPost)
            {
                comment.ParentPost.Comments = new List<DB4Thing>();

                Task.Run(async () =>
                {
                    var postComments = await parentPost.GetCommentsAsync();

                    foreach (var postComment in postComments)
                    {
                        comment.ParentPost.Comments.Add(RedditThingConverter.Convert(postComment));
                    }
                }).Wait();
            }

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

        public void ReplyToThing(DB4Thing thing, string reply, bool isSticky = false)
        {
            var qualifiedThing = getQualifiedThing(thing);

            Task.Run(async () =>
            {
                Comment newComment = null;

                if (qualifiedThing is Post post)
                {
                    // Make a new comment
                    newComment = await post.CommentAsync(reply);
                }
                else if (qualifiedThing is Comment comment)
                {
                    // Reply to existing comment
                    newComment = await comment.ReplyAsync(reply);
                }
                else
                {
                    throw new InvalidOperationException($"Tried to reply to a Thing that isn't a Post or Comment - Thing ID: {qualifiedThing.Id}");
                }

                // All DB4 replies should be distinguished
                await newComment.DistinguishAsync(ModeratableThing.DistinguishType.Moderator, isSticky);

            }).Wait();
        }

        public void EditComment(DB4Thing comment, string editedComment)
        {
            // Can only edit comments
            Assert.That(comment.Type == DB4ThingType.Comment);

            var qualifiedComment = (Comment)getQualifiedThing(comment);
            Task.Run(async () => await qualifiedComment.EditTextAsync(editedComment)).Wait();
        }

        public void DeleteComment(DB4Thing comment)
        {
            // Can only delete comments
            Assert.That(comment.Type == DB4ThingType.Comment);

            var qualifiedComment = (Comment)getQualifiedThing(comment);
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

        private Thing getQualifiedThing(DB4Thing thing)
        {
            var link = thing.Type == DB4ThingType.Comment ? thing.Shortlink : thing.Permalink;

            string thingUrl = $"{OAuthRedditBaseUrl}{link}".TrimEnd('/');
            var thingUri = new Uri(thingUrl);

            switch (thing.Type)
            {
                case DB4ThingType.Comment:
                    // Get post
                    return _reddit.GetCommentAsync(thingUri).Result;
                case DB4ThingType.Post:
                    // Get comment with children and parent post populated
                    return _reddit.GetPostAsync(thingUri).Result;
                default:
                    throw new UnhandledEnumException<DB4ThingType>(thing.Type);
            }
        }

        private PrivateMessage getPrivateMessageById(string privateMessageId)
        {
            return _reddit.User.GetInbox().Where(pm => pm != null && pm.Id == privateMessageId && pm.Unread).FirstOrDefault().Result;
        }
    }
}
