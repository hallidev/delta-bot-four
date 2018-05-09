namespace DeltaBotFour.Models
{
    public class DB4Comment
    {
        public DB4CommentType CommentType { get; set; }
        public string CommentBody { get; set; }
        public bool IsValidDelta => CommentType == DB4CommentType.SuccessDeltaAwarded;
    }
}
