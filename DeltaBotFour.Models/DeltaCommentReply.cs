namespace DeltaBotFour.Models
{
    public class DeltaCommentReply
    {
        public DeltaCommentReplyType ResultType { get; set; }
        public string ReplyCommentBody { get; set; }
        public bool IsValidDelta => ResultType == DeltaCommentReplyType.SuccessDeltaAwarded;
        public bool IsModeratorReply => ResultType == DeltaCommentReplyType.ModeratorRemoved;
    }
}
