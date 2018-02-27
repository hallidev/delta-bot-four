using DeltaBotFour.Models;

namespace DeltaBotFour.ServiceInterfaces
{
    public interface IDeltaAwarder
    {
        void Award(DB4Thing comment, DB4Thing parentComment);
        void Unaward(DB4Thing comment, DB4Thing parentComment);
    }
}
