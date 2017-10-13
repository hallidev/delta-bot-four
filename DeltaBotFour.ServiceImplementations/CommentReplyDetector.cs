using DeltaBotFour.Models;
using DeltaBotFour.ServiceInterfaces;

namespace DeltaBotFour.ServiceImplementations
{
    public class CommentReplyDetector : ICommentReplyDetector
    {
        public CommentReplyDetector()
        {

        }

        public bool DidDB4Reply(CommentComposite commentComposite)
        {
            // Check for a reply in the immediate children

            return false;
        }
    }
}
