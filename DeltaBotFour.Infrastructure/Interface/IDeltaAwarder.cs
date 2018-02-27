using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface IDeltaAwarder
    {
        void Award(DB4Thing comment);
        void Unaward(DB4Thing comment);
    }
}
