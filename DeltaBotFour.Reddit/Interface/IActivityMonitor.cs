namespace DeltaBotFour.Reddit.Interface
{
    public interface IActivityMonitor
    {
        void Start(int editScanIntervalSeconds);
        void Stop();
    }
}
