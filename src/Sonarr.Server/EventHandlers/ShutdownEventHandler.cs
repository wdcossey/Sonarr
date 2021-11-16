using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace Sonarr.Server.EventHandlers
{
    public class ShutdownEventHandler: IHandle<ApplicationShutdownRequested>
    {
        private readonly ILogger<ShutdownEventHandler> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IRuntimeInfo _runtimeInfo;

        public ShutdownEventHandler(
            ILogger<ShutdownEventHandler> logger, 
            IHostApplicationLifetime applicationLifetime,
            IRuntimeInfo runtimeInfo)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _runtimeInfo = runtimeInfo;
        }
        
        public void Handle(ApplicationShutdownRequested message)
        {
            if (!_runtimeInfo.IsWindowsService)
            {
                if (message.Restarting)
                {
                    _runtimeInfo.RestartPending = true;
                }

                LogManager.Configuration = null;
                Shutdown();
            }
        }
        
        private void Shutdown()
        {
            _logger.LogInformation("Attempting to stop application");
            _applicationLifetime.StopApplication();
            _logger.LogInformation("Application has finished stop routine");
            _runtimeInfo.IsExiting = true;
        }
    }
}
