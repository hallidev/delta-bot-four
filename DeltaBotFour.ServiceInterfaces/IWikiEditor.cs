using RedditSharp.Things;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface IWikiEditor
    {
        void CreateWikiEntry(Comment comment, Comment parentComment);
        void EditWikiEntry(Comment comment, Comment parentComment);
    }
}
