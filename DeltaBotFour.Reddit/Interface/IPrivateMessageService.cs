namespace DeltaBotFour.Reddit.Interface
{
    public interface IPrivateMessageService
    {
        void SetAsRead(string fullName, string id);
    }
}
