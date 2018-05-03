using RedditSharp.Things;

namespace DeltaBotFour.Reddit.Interface
{
    public interface IActivityDispatcher
    {
        void SendToQueue(Comment comment);
        void SendToQueue(PrivateMessage privateMessage);
    }
}
