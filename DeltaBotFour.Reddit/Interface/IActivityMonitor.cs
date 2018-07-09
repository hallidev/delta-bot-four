namespace DeltaBotFour.Reddit.Interface
{
    public interface IActivityMonitor
    {
        void Start(int editScanIntervalSeconds, int pmScanIntervalSeconds);
        void Stop();
    }
}
