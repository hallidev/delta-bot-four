using DeltaBotFour.ServiceInterfaces;
using DeltaBotFour.Models;
using RedditSharp.Things;
using System.IO;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentReplier : ICommentReplier
    {
        private AppConfiguration _appConfiguration;
        private string _replyTemplate;

        public CommentReplier(AppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;
        }

        public async void Reply(Comment comment, DeltaCommentValidationResult deltaCommentValidationResult)
        {
            string replyMessage = getReplyMessage(deltaCommentValidationResult);

            await comment.ReplyAsync(replyMessage);
        }

        public async void EditReply(Comment commentToEdit, DeltaCommentValidationResult deltaCommentValidationResult)
        {
            string replyMessage = getReplyMessage(deltaCommentValidationResult);

            await commentToEdit.EditTextAsync(replyMessage);
        }

        public async void DeleteReply(Comment commentToDelete)
        {
            await commentToDelete.DelAsync();
        }

        private string getReplyMessage(DeltaCommentValidationResult deltaCommentValidationResult)
        {
            if(string.IsNullOrEmpty(_replyTemplate))
            {
                // Load reply template
                _replyTemplate = File.ReadAllText(_appConfiguration.DB4ReplyTemplateFile);
            }

            // TODO: Fix footer
            return _replyTemplate.Replace(_appConfiguration.ReplaceTokens.DB4ReplyToken, deltaCommentValidationResult.ReplyCommentBody);
        }
    }
}
