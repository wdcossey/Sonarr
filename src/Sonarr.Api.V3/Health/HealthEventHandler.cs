using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.Health
{
    public class HealthEventHandler: EventHandlerBase<HealthResource>, IHandleAsync<HealthCheckCompleteEvent>
    {
        public HealthEventHandler(IHubContext<SonarrHub, ISonarrHub> hubContext) 
            : base(hubContext) { }

        public Task HandleAsync(HealthCheckCompleteEvent message)
            => BroadcastResourceChange(ModelAction.Sync);
    }
}
