using System;
using DeltaBotFour.Models;
using RedditSharp.Things;

namespace DeltaBotFour.Reddit
{
    internal static class RedditThingConverter
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
                    Kind = post.Kind,
                    Id = post.Id,
                    FullName = post.FullName,
                    AuthorName = post.AuthorName,
                    AuthorFlairText = post.AuthorFlairText,
                    AuthorFlairCssClass = post.AuthorFlairCssClass,
                    Created = post.Created,
                    CreatedUtc = post.CreatedUTC,
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
                    Kind = comment.Kind,
                    Id = comment.Id,
                    ParentId = comment.ParentId,
                    FullName = comment.FullName,
                    AuthorName = comment.AuthorName,
                    AuthorFlairText = comment.AuthorFlairText,
                    AuthorFlairCssClass = comment.AuthorFlairCssClass,
                    Body = comment.Body,
                    Created = comment.Created,
                    CreatedUtc = comment.CreatedUTC,
                    Permalink = comment.Permalink.OriginalString,
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
                    Kind = privateMessage.Kind,
                    Id = privateMessage.Id,
                    ParentId = privateMessage.ParentID,
                    FullName = privateMessage.FullName,
                    AuthorName = privateMessage.AuthorName,
                    Subject = privateMessage.Subject,
                    Body = privateMessage.Body,
                    Unread = privateMessage.Unread
                };
            }

            throw new Exception($"Trying to convert unknown Reddit Thing Type: {thing.GetType()}");
        }
    }
}
