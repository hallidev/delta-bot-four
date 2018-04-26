using DeltaBotFour.Models;

namespace DeltaBotFour.Reddit.Interface
{
    public interface IRedditService
    {
        void PopulateParentAndChildren(DB4Thing comment);
        DB4Thing GetThingByFullname(string fullname);
        void ReplyToComment(DB4Thing comment, string reply);
        void EditComment(DB4Thing comment, string editedComment);
        void DeleteComment(DB4Thing comment);
    }
}
