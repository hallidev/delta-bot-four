namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IDeltaLogEditor
    {
        string Upsert(string mainSubPostId, string mainSubPostPermalink, string mainSubPostTitle, string opUsername);
    }
}
