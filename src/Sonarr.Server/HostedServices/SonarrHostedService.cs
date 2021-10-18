using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace Sonarr.Server.HostedServices
{
    public class SonarrHostedService : IHostedService
    {
        private readonly ILogger<SonarrHostedService> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IEventAggregator _eventAggregator;

        public SonarrHostedService(ILogger<SonarrHostedService> logger, IHostApplicationLifetime applicationLifetime, IEventAggregator eventAggregator)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _eventAggregator = eventAggregator;
            //app.ApplicationServices.GetRequiredService<IEventAggregator>().PublishEvent(new ApplicationStartingEvent());

            //app.ApplicationServices.GetRequiredService<IEventAggregator>().PublishEvent(new ApplicationStartedEvent());
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventAggregator.PublishEvent(new ApplicationStartingEvent());

            _applicationLifetime.ApplicationStarted.Register(() => ApplicationStartedCallback(cancellationToken));
            _applicationLifetime.ApplicationStopping.Register(() => ApplicationApplicationStopping(cancellationToken));
            _applicationLifetime.ApplicationStopped.Register(ApplicationApplicationStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void ApplicationStartedCallback(CancellationToken cancellationToken)
        {
            _eventAggregator.PublishEvent(new ApplicationStartedEvent());
        }

        private void ApplicationApplicationStopping(CancellationToken cancellationToken)
        {
            _eventAggregator.PublishEvent(new ApplicationShutdownRequested());
        }

        private void ApplicationApplicationStopped()
        {

        }


    }
}
