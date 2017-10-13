namespace DeltaBotFour.Models
{
    public class DeltaCommentValidationResult
    {
        public DeltaCommentValidationResultType ResultType { get; set; }
        public string ReplyCommentBody { get; set; }
        public bool IsValidDelta => ResultType == DeltaCommentValidationResultType.SuccessDeltaAwarded;
    }
}
