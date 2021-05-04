using System;
using System.Threading.Tasks;
using DeltaBotFour.Shared.Logging;

namespace DeltaBotFour.Reddit
{
    public class RedditState
    {
        private const int EditSeconds = 10;
        
        private readonly ILogger _logger;
        private DateTimeOffset _nextEditUtc;

        public RedditState(ILogger logger)
        {
            _logger = logger;
        }
        
        public async Task AcquireEditLock()
        {
            var waitMs = (int) (_nextEditUtc - DateTimeOffset.UtcNow).TotalMilliseconds;
            
            if (waitMs > 0)
            {
                _logger.Info($"Waiting for {waitMs}ms before performing edit...");
                await Task.Delay(waitMs);
            }

            _nextEditUtc = DateTimeOffset.UtcNow.AddSeconds(EditSeconds);
        }
    }
}
