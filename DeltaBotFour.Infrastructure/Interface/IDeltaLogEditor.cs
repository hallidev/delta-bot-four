namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IDeltaLogEditor
    {
        void UpsertOrRemove(string postId);
    }
}
