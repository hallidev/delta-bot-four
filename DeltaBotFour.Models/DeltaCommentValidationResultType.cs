namespace DeltaBotFour.Models
{
    public enum DeltaCommentValidationResultType
    {
        FailCommentTooShort,
        FailCannotAwardOP,
        FailCannotAwardDeltaBot,
        FailCannotAwardSelf,
        FailRejected,
        SuccessDeltaAwarded
    }
}
