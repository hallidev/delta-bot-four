using DeltaBotFour.ServiceInterfaces;
using DeltaBotFour.Models;
using RedditSharp.Things;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentReplier : ICommentReplier
    {
        private AppConfiguration _appConfiguration;

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
            // TODO: Fix footer
            return $"{deltaCommentValidationResult.ReplyCommentBody}{_appConfiguration.ReplyFooter}";
        }
    }
}
