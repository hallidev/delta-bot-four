using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Implementation
{
    public class RedditSharpThingService : IRedditThingService
    {
        private readonly RedditSharp.Reddit _reddit;

        public RedditSharpThingService(RedditSharp.Reddit reddit)
        {
            _reddit = reddit;
        }

        public void PopulateParentAndChildren(DB4Thing comment)
        {
            // Get comment with children and parent post populated
            var qualifiedComment = _reddit.GetCommentAsync(new Uri(comment.Shortlink)).Result;

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
            var qualifiedComment = _reddit.GetCommentAsync(new Uri(comment.Shortlink)).Result;
            Task.Run(async () => await qualifiedComment.ReplyAsync(reply)).Wait();
        }

        public void EditComment(DB4Thing comment, string editedComment)
        {
            var qualifiedComment = _reddit.GetCommentAsync(new Uri(comment.Shortlink)).Result;
            Task.Run(async () => await qualifiedComment.EditTextAsync(editedComment)).Wait();
        }

        public void DeleteComment(DB4Thing comment)
        {
            var qualifiedComment = _reddit.GetCommentAsync(new Uri(comment.Shortlink)).Result;
            Task.Run(async () => await qualifiedComment.DelAsync());
        }
    }
}
