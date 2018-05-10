namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IDeltaLogEditor
    {
        string Upsert(string postId, string postPermalink);
    }
}
