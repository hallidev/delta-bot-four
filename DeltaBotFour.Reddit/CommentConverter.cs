using System;
using DeltaBotFour.Models;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit
{
    public static class CommentConverter
    {
        private const string SHORT_LINK_FROM = "www.reddit.com";
        private const string SHORT_LINK_TO = "oauth.reddit.com";

        public static DB4Thing Convert(Thing thing)
        {
            if (thing is Post post)
            {
                return new DB4Thing
                {
                    Id = post.Id,
                    Type = DB4ThingType.Post,
                    AuthorName = post.AuthorName,
                    AuthorFlairText = post.AuthorFlairText,
                    AuthorFlairCssClass = post.AuthorFlairCssClass,
                    Created = post.Created,
                    CreatedUTC = post.CreatedUTC,
                    Permalink = post.Permalink.OriginalString,
                    Title = post.Title,
                    Shortlink = post.Shortlink.Replace(SHORT_LINK_FROM, SHORT_LINK_TO),
                };
            }

            if (thing is Comment comment)
            {
                return new DB4Thing
                {
                    Id = comment.Id,
                    ParentId = comment.ParentId,
                    Type = DB4ThingType.Comment,
                    AuthorName = comment.AuthorName,
                    AuthorFlairText = comment.AuthorFlairText,
                    AuthorFlairCssClass = comment.AuthorFlairCssClass,
                    Body = comment.Body,
                    Created = comment.Created,
                    CreatedUTC = comment.CreatedUTC,
                    IsEdited = comment.Edited,
                    Shortlink = comment.Shortlink.Replace(SHORT_LINK_FROM, SHORT_LINK_TO)
                };
            }

            throw new Exception($"Trying to convert unknown Reddit Thing Type: {thing.GetType()}");
        }
    }
}
