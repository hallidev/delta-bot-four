namespace DeltaBotFour.Reddit.Interface
{
    public interface IFlairEditor
    {
        void SetUserFlair(string username, string cssClass, string flairText);
    }
}
