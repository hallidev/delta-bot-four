namespace DeltaBotFour.Models
{
    public class DB4ReplyResult
    {
        public bool HasDB4Replied { get; set; }
        public bool WasSuccessReply => CommentType.HasValue && CommentType.Value == DB4CommentType.SuccessDeltaAwarded;
        public bool WasModeratorReply => CommentType.HasValue && (CommentType.Value == DB4CommentType.ModeratorAdded || CommentType == DB4CommentType.ModeratorRemoved);
        public DB4CommentType? CommentType { get; set; }
        public DB4Thing Comment { get; set; }
    }
}
