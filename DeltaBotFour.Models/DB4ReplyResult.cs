namespace DeltaBotFour.Models
{
    public class DB4ReplyResult
    {
        public bool HasDB4Replied { get; set; }
        public bool WasSuccessReply { get; set; }
        public bool WasModeratorReply { get; set; }
        public DB4CommentType CommentType { get; set; }
        public DB4Thing Comment { get; set; }
    }
}
