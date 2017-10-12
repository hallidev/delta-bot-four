namespace DeltaBotFour.Models
{
    public class DeltaCommentValidationResult
    {
        public DeltaCommentValidationResultType ResultType { get; set; }
        public string ReplyCommentBody { get; set; }
        public bool IsValidDelta => ResultType == DeltaCommentValidationResultType.Success;

        // Can only create instances through use of the Create() method
        private DeltaCommentValidationResult()
        {

        }

        public static void Create(DeltaCommentValidationResultType resultType)
        {

        }
    }
}
