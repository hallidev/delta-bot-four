using DeltaBotFour.Models;

namespace DeltaBotFour.Reddit.Interface
{
    public interface IRedditService
    {
        void PopulateParentAndChildren(DB4Thing comment);
        void PopulateChildren(DB4Thing post);
        DB4Thing GetThingByFullname(string fullname);
        DB4Thing GetCommentByUrl(string url);
        void ReplyToThing(DB4Thing thing, string reply, bool isSticky = false);
        void EditComment(DB4Thing comment, string editedComment);
        void DeleteComment(DB4Thing comment);
        void SendPrivateMessage(string subject, string body, string to, string fromSubreddit = "");
        void ReplyToPrivateMessage(string privateMessageId, string body);
        void SetPrivateMessageAsRead(string privateMessageId);
    }
}
