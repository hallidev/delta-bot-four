namespace DeltaBotFour.Models
{
    public class DB4Comment
    {
        public DB4CommentType ResultType { get; set; }
        public string ReplyCommentBody { get; set; }
        public bool IsValidDelta => ResultType == DB4CommentType.SuccessDeltaAwarded;
        public bool IsModeratorReply => ResultType == DB4CommentType.ModeratorAdded || ResultType == DB4CommentType.ModeratorRemoved;
    }
}
