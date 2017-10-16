using RedditSharp.Things;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentDispatcher
    {
        void SendToQueue(Comment comment);
    }
}
