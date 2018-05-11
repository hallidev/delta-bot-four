using System;

namespace DeltaBotFour.Shared.Logging
{
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(Exception ex, string message = "");
    }
}
