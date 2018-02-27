using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Interface
{
    public interface ICommentDispatcher
    {
        void SendToQueue(Comment comment);
    }
}
