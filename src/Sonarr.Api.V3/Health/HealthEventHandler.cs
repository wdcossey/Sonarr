using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.Health
{
    public class HealthEventHandler: EventHandlerBase<HealthResource>,
                                     IHandle<HealthCheckCompleteEvent>
    {
        public HealthEventHandler(IHubContext<SonarrHub, ISonarrHub> hubContext)
            : base(hubContext) { }

        public void Handle(HealthCheckCompleteEvent message)
            => BroadcastResourceChange(ModelAction.Sync);
    }
}
