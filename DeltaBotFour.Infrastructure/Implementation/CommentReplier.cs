using System;
using System.IO;
using Core.Foundation.Helpers;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Models;
using DeltaBotFour.Reddit.Interface;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class CommentReplier : ICommentReplier
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly IRedditService _redditService;
        private string _replyTemplate;

        public CommentReplier(AppConfiguration appConfiguration, IRedditService redditService)
        {
            _appConfiguration = appConfiguration;
            _redditService = redditService;
        }

        public void Reply(DB4Thing thing, DB4Comment db4Comment, bool isSticky = false)
        {
            string replyMessage = getReplyMessage(db4Comment);

            _redditService.ReplyToThing(thing, replyMessage, isSticky);

            ConsoleHelper.WriteLine($"DeltaBot replied -> result: {db4Comment.CommentType.ToString()} link: {thing.Shortlink}");
        }

        public void EditReply(DB4Thing commentToEdit, DB4Comment db4Comment)
        {
            string replyMessage = getReplyMessage(db4Comment);

            _redditService.EditComment(commentToEdit, replyMessage);

            ConsoleHelper.WriteLine($"DeltaBot edited a reply -> result: {db4Comment.CommentType.ToString()} link: {commentToEdit.Shortlink}");
        }

        public void DeleteReply(DB4Thing commentToDelete)
        {
            _redditService.DeleteComment(commentToDelete);

            ConsoleHelper.WriteLine($"DeltaBot deleted a reply -> link: {commentToDelete.Shortlink}");
        }

        private string getReplyMessage(DB4Comment db4Comment)
        {
            if(string.IsNullOrEmpty(_replyTemplate))
            {
                // Load reply template
                _replyTemplate = File.ReadAllText(_appConfiguration.TemplateFiles.DB4CommentTemplateFile)
                    .Replace(Environment.NewLine, "\n");
            }

            return _replyTemplate
                .Replace(_appConfiguration.ReplaceTokens.DB4ReplyToken, db4Comment.CommentBody)
                .Replace(_appConfiguration.ReplaceTokens.SubredditToken, _appConfiguration.SubredditName);
        }
    }
}
