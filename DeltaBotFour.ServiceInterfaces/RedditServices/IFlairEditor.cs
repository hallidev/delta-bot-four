namespace DeltaBotFour.ServiceInterfaces.RedditServices
{
    public interface IFlairEditor
    {
        void SetUserFlair(string username, string cssClass, string flairText);
    }
}
