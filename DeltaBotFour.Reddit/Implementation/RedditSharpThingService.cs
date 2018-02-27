using System;
using System.Collections.Generic;
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
            comment.ParentPost = CommentConverter.Convert(qualifiedComment.Parent);

            // Convert immediate children only
            var childComments = new List<DB4Thing>();

            foreach (Comment childComment in qualifiedComment.Comments)
            {
                childComments.Add(CommentConverter.Convert(childComment));
            }

            comment.Comments = childComments;

            // Get the parent thing - this could be the same as ParentPost above or it could be a comment
            var parentThing = _reddit.GetThingByFullnameAsync(comment.ParentId).Result;
            comment.ParentThing = CommentConverter.Convert(parentThing);
        }

        public void ReplyToComment(DB4Thing comment, string reply)
        {
            throw new System.NotImplementedException();
        }

        public void EditComment(DB4Thing comment, string editedComment)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteComment(DB4Thing comment)
        {
            throw new System.NotImplementedException();
        }
    }
}
