using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IDeltaboardBuilder
    {
        void Build(DeltaboardType type);
    }
}
