using System;
using NLog;
using ILogger = DeltaBotFour.Shared.Logging.ILogger;

namespace DeltaBotFour.Shared.Implementation
{
    public class NLogProxy<T> : ILogger
    {
        private static readonly NLog.ILogger Logger = LogManager.GetLogger(typeof(T).FullName);

        public void Info(string message)
        {
            Logger.Info(message);
        }

        public void Warn(string message)
        {
            Logger.Warn(message);
        }

        public void Error(Exception ex, string message = "")
        {
            if (string.IsNullOrEmpty(message)) message = ex.Message;
            Logger.Error(ex, message);
        }
    }
}
