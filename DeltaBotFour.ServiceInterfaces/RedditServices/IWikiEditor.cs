namespace DeltaBotFour.ServiceInterfaces.RedditServices
{
    public interface IWikiEditor
    {
        string GetPage(string url);
        void EditPage(string url, string content);
    }
}
