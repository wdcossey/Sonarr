using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace Sonarr.Server.EventHandlers
{
    public class ShutdownEventHandler: IHandleAsync<ApplicationShutdownRequested>
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
        
        public Task HandleAsync(ApplicationShutdownRequested message)
        {
            if (!_runtimeInfo.IsWindowsService)
            {
                if (message.Restarting)
                {
                    _runtimeInfo.RestartPending = true;
                }

                NLog.LogManager.Configuration = null;
                Shutdown();
            }
            
            return Task.CompletedTask;
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
