using System;
using DeltaBotFour.Models;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit
{
    public static class RedditThingConverter
    {
        private const string SHORT_LINK_FROM = "www.reddit.com";
        private const string SHORT_LINK_TO = "oauth.reddit.com";

        public static DB4Thing Convert(Thing thing)
        {
            if (thing is Post post)
            {
                return new DB4Thing
                {
                    Type = DB4ThingType.Post,
                    Id = post.Id,
                    AuthorName = post.AuthorName,
                    AuthorFlairText = post.AuthorFlairText,
                    AuthorFlairCssClass = post.AuthorFlairCssClass,
                    Created = post.Created,
                    CreatedUTC = post.CreatedUTC,
                    FullName = post.FullName,
                    Permalink = post.Permalink.OriginalString,
                    Title = post.Title,
                    Shortlink = post.Shortlink.Replace(SHORT_LINK_FROM, SHORT_LINK_TO),
                };
            }

            if (thing is Comment comment)
            {
                return new DB4Thing
                {
                    Type = DB4ThingType.Comment,
                    Id = comment.Id,
                    ParentId = comment.ParentId,
                    AuthorName = comment.AuthorName,
                    AuthorFlairText = comment.AuthorFlairText,
                    AuthorFlairCssClass = comment.AuthorFlairCssClass,
                    Body = comment.Body,
                    Created = comment.Created,
                    CreatedUTC = comment.CreatedUTC,
                    FullName = comment.FullName,
                    IsEdited = comment.Edited,
                    LinkId = comment.LinkId,
                    Shortlink = comment.Shortlink.Replace(SHORT_LINK_FROM, SHORT_LINK_TO),
                    Subreddit = comment.Subreddit
                };
            }

            if (thing is PrivateMessage privateMessage)
            {
                return new DB4Thing
                {
                    Type = DB4ThingType.PrivateMessage,
                    Id = privateMessage.Id,
                    ParentId = privateMessage.ParentID,
                    AuthorName = privateMessage.AuthorName,
                    Subject = privateMessage.Subject,
                    Body = privateMessage.Body,
                };
            }

            throw new Exception($"Trying to convert unknown Reddit Thing Type: {thing.GetType()}");
        }
    }
}
