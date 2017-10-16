using RedditSharp.Things;

namespace DeltaBotFour.Models
{
    public class DB4ReplyResult
    {
        public bool HasDB4Replied { get; set; }
        public bool WasSuccessReply { get; set; }
        public Comment Comment { get; set; }
    }
}
