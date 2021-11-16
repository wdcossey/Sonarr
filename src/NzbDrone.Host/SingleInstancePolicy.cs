using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Processes;

namespace NzbDrone.Host
{
    public interface ISingleInstancePolicy
    {
        void PreventStartIfAlreadyRunning();
        void KillAllOtherInstance();
        void WarnIfAlreadyRunning();
    }

    public class SingleInstancePolicy : ISingleInstancePolicy
    {
        private readonly IProcessProvider _processProvider;
        private readonly IBrowserService _browserService;
        private readonly ILogger<SingleInstancePolicy> _logger;

        public SingleInstancePolicy(IProcessProvider processProvider,
                                    IBrowserService browserService,
                                    ILogger<SingleInstancePolicy> logger)
        {
            _processProvider = processProvider;
            _browserService = browserService;
            _logger = logger;
        }

        public void PreventStartIfAlreadyRunning()
        {
            if (IsAlreadyRunning())
            {
                _logger.LogWarning("Another instance of Sonarr is already running.");
                _browserService.LaunchWebUI();
                throw new TerminateApplicationException("Another instance is already running");
            }
        }

        public void KillAllOtherInstance()
        {
            foreach (var processId in GetOtherNzbDroneProcessIds())
            {
                _processProvider.Kill(processId);
            }
        }

        public void WarnIfAlreadyRunning()
        {
            if (IsAlreadyRunning())
            {
                _logger.LogDebug("Another instance of Sonarr is already running.");
            }
        }

        private bool IsAlreadyRunning()
        {
            return GetOtherNzbDroneProcessIds().Any();
        }

        private List<int> GetOtherNzbDroneProcessIds()
        {
            try
            {
                var currentId = _processProvider.GetCurrentProcess().Id;

                var otherProcesses = _processProvider.FindProcessByName(ProcessProvider.SONARR_CONSOLE_PROCESS_NAME)
                                                     .Union(_processProvider.FindProcessByName(ProcessProvider.SONARR_PROCESS_NAME))
                                                     .Select(c => c.Id)
                                                     .Except(new[] { currentId })
                                                     .ToList();

                if (otherProcesses.Any())
                {
                    _logger.LogInformation("{Count} instance(s) of Sonarr are running", otherProcesses.Count);
                }

                return otherProcesses;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check for multiple instances of Sonarr.");
                return new List<int>();
            }
        }
    }
}
