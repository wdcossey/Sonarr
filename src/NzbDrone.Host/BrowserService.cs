using System;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Host
{
    public interface IBrowserService
    {
        void LaunchWebUI();
    }

    public class BrowserService : IBrowserService
    {
        private readonly IProcessProvider _processProvider;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IRuntimeInfo _runtimeInfo;
        private readonly ILogger<BrowserService> _logger;

        public BrowserService(IProcessProvider processProvider,
                              IConfigFileProvider configFileProvider,
                              IRuntimeInfo runtimeInfo,
                              ILogger<BrowserService> logger)
        {
            _processProvider = processProvider;
            _configFileProvider = configFileProvider;
            _runtimeInfo = runtimeInfo;
            _logger = logger;
        }

        public void LaunchWebUI()
        {
            var url = $"http://localhost:{_configFileProvider.Port}";
            try
            {
                if (_runtimeInfo.IsUserInteractive)
                {
                    _logger.LogInformation("Starting default browser. {Url}", url);
                    _processProvider.OpenDefaultBrowser(url);
                }
                else
                {
                    _logger.LogDebug("non-interactive runtime. Won't attempt to open browser.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Couldn't open default browser to {Url}", url);
            }
        }
    }
}