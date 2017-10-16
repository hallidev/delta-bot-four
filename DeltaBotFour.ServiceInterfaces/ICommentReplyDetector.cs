using DeltaBotFour.Models;
using RedditSharp.Things;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentReplyDetector
    {
        DB4ReplyResult DidDB4Reply(Comment comment);
    }
}
