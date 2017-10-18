using RedditSharp.Things;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface IUserWikiEditor
    {
        void UpdateUserWikiEntryAward(Comment comment, Comment parentComment);
        void UpdateUserWikiEntryUnaward(Comment comment, Comment parentComment);
    }
}
