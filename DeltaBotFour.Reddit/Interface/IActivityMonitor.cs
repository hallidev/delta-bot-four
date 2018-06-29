namespace DeltaBotFour.Reddit.Interface
{
    public interface IActivityMonitor
    {
        void Start(int commentScanIntervalSeconds, int editScanIntervalSeconds, int pmScanIntervalSeconds);
        void Stop();
    }
}
