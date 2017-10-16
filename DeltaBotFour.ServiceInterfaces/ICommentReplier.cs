using DeltaBotFour.Models;
using RedditSharp.Things;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentReplier
    {
        void Reply(Comment comment, DeltaCommentValidationResult deltaCommentValidationResult);
    }
}
