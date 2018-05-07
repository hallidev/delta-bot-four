using DeltaBotFour.Models;

namespace DeltaBotFour.Reddit.Interface
{
    public interface IRedditService
    {
        void PopulateParentAndChildren(DB4Thing comment);
        DB4Thing GetThingByFullname(string fullname);
        DB4Thing GetCommentByUrl(string url);
        void ReplyToComment(DB4Thing comment, string reply);
        void EditComment(DB4Thing comment, string editedComment);
        void DeleteComment(DB4Thing comment);
        void SendPrivateMessage(string subject, string body, string to, string fromSubreddit = "");
        void ReplyToPrivateMessage(string privateMessageId, string body);
        void SetPrivateMessageAsRead(string privateMessageId);
    }
}
