namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IDeltaboardEditor
    {
        void AddDelta(string username);
        void RemoveDelta(string username);
        void RefreshDeltaboards();
    }
}
