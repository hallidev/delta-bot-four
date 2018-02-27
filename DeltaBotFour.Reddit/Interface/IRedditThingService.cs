using DeltaBotFour.Models;

namespace DeltaBotFour.Reddit.Interface
{
    public interface IRedditThingService
    {
        void PopulateParentAndChildren(DB4Thing comment);
        void ReplyToComment(DB4Thing comment, string reply);
        void EditComment(DB4Thing comment, string editedComment);
        void DeleteComment(DB4Thing comment);
    }
}
