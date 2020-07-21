using System;
using System.Diagnostics;
using DeltaBotFour.Shared.Logging;

namespace DeltaBotFour.Shared
{
    /// <summary>
    /// The bot stops processing PMs after 24hrs or so. I've never been able to re-create it, so this class
    /// manages an automatic restart
    /// </summary>
    public class AutoRestartManager
    {
        private readonly ILogger _logger;
        private int _inFlightCount;
        private int _automaticRestartHours;
        private DateTime _startTime;

        public bool RequiresRestart
        {
            get
            {
                var timeRunning = DateTime.UtcNow - _startTime;
                return timeRunning.TotalHours >= _automaticRestartHours;
            }
        }

        public AutoRestartManager(ILogger logger)
        {
            _logger = logger;
        }

        public void Start(int automaticRestartHours)
        {
            _automaticRestartHours = automaticRestartHours;
            _startTime = DateTime.UtcNow;
        }

        public void AddInFlight()
        {
            _inFlightCount++;
        }

        public void RemoveInFlight()
        {
            _inFlightCount--;
        }

        public void RestartIfNecessary()
        {
            if (RequiresRestart)
            {
                _logger.Info($"Auto restart required. In flight requests: {_inFlightCount}");

                if (_inFlightCount == 0)
                {
                    _logger.Info("Restarting...");
                    var dllName = AppDomain.CurrentDomain.FriendlyName;
                    Process.Start("dotnet", $"{dllName}.dll");
                    Environment.Exit(0);
                }
            }
        }
    }
}
