using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DeltaBotFour.Infrastructure.Interface;
using DeltaBotFour.Shared.Logging;

namespace DeltaBotFour.Infrastructure.Implementation
{
    public class HealthPinger : IHealthPinger
    {
        private readonly AppConfiguration _configuration;
        private readonly ILogger _logger;

        private HttpClient _httpClient;
        private CancellationTokenSource _cancellationTokenSource;
        
        public HealthPinger(AppConfiguration configuration,
            ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Start()
        {
            _httpClient = new HttpClient();
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    try
                    {
                        var result = await _httpClient.GetAsync(new Uri(_configuration.HealthCheckUrl));

                        if (!result.IsSuccessStatusCode)
                        {
                            _logger.Warn($"Health check service result non-success status code: {result.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Error while attempting to ping health service: {ex.Message}");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(_configuration.HealthCheckIntervalMinutes));
                }
            }, _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
