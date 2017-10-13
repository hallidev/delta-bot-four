using DeltaBotFour.Models;
using RedditSharp.Things;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface ICommentValidator
    {
        DeltaCommentValidationResult Validate(Comment comment, Thing parentThing);
    }
}
