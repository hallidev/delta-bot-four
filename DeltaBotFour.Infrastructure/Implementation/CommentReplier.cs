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

        public void Reply(DB4Thing comment, DeltaCommentValidationResult deltaCommentValidationResult)
        {
            string replyMessage = getReplyMessage(deltaCommentValidationResult);

            _redditService.ReplyToComment(comment, replyMessage);

            ConsoleHelper.WriteLine($"DeltaBot replied -> result: {deltaCommentValidationResult.ResultType.ToString()} link: {comment.Shortlink}");
        }

        public void EditReply(DB4Thing commentToEdit, DeltaCommentValidationResult deltaCommentValidationResult)
        {
            string replyMessage = getReplyMessage(deltaCommentValidationResult);

            _redditService.EditComment(commentToEdit, replyMessage);

            ConsoleHelper.WriteLine($"DeltaBot edited a reply -> result: {deltaCommentValidationResult.ResultType.ToString()} link: {commentToEdit.Shortlink}");
        }

        public void DeleteReply(DB4Thing commentToDelete)
        {
            _redditService.DeleteComment(commentToDelete);

            ConsoleHelper.WriteLine($"DeltaBot deleted a reply -> link: {commentToDelete.Shortlink}");
        }

        private string getReplyMessage(DeltaCommentValidationResult deltaCommentValidationResult)
        {
            if(string.IsNullOrEmpty(_replyTemplate))
            {
                // Load reply template
                _replyTemplate = File.ReadAllText(_appConfiguration.TemplateFiles.DB4ReplyTemplateFile)
                    .Replace(Environment.NewLine, "\n");
            }

            return _replyTemplate.Replace(_appConfiguration.ReplaceTokens.DB4ReplyToken, deltaCommentValidationResult.ReplyCommentBody);
        }
    }
}
