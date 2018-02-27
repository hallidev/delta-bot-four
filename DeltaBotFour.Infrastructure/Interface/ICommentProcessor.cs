using DeltaBotFour.Models;

namespace DeltaBotFour.Infrastructure.Interface
{
    public interface ICommentProcessor
    {
        void Process(DB4Thing comment);
    }
}
