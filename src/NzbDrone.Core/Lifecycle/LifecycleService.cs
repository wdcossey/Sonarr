using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Lifecycle.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using IServiceProvider = NzbDrone.Common.IServiceProvider;

namespace NzbDrone.Core.Lifecycle
{
    public interface ILifecycleService
    {
        void Shutdown();
        void Restart();
    }

    public class LifecycleService : ILifecycleService, IExecuteAsync<ShutdownCommand>, IExecuteAsync<RestartCommand>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRuntimeInfo _runtimeInfo;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LifecycleService> _logger;


        public LifecycleService(IEventAggregator eventAggregator,
                                IRuntimeInfo runtimeInfo,
                                IServiceProvider serviceProvider,
                                ILogger<LifecycleService> logger)
        {
            _eventAggregator = eventAggregator;
            _runtimeInfo = runtimeInfo;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void Shutdown()
        {
            _logger.LogInformation("Shutdown requested.");
            _eventAggregator.PublishEvent(new ApplicationShutdownRequested());

            if (_runtimeInfo.IsWindowsService)
            {
                //TODO: _serviceProvider.Stop()
                throw new NotImplementedException("_serviceProvider.Stop()");
                _serviceProvider.Stop(ServiceProvider.SERVICE_NAME);
            }
        }

        public void Restart()
        {
            _logger.LogInformation("Restart requested.");

            _eventAggregator.PublishEvent(new ApplicationShutdownRequested(true));

            if (_runtimeInfo.IsWindowsService)
            {
                //TODO: _serviceProvider.Restart()
                throw new NotImplementedException("_serviceProvider.Restart()");
                _serviceProvider.Restart(ServiceProvider.SERVICE_NAME);
            }
        }

        public Task ExecuteAsync(ShutdownCommand message)
        {
            Shutdown();
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(RestartCommand message)
        {
            Restart();
            return Task.CompletedTask;
        }
    }
}
