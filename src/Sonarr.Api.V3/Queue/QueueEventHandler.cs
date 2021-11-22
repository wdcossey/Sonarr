using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Queue;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.Queue
{
    public class QueueEventHandler: EventHandlerBase<QueueResource>, IHandleAsync<QueueUpdatedEvent>, IHandleAsync<PendingReleasesUpdatedEvent>
    {
        public QueueEventHandler(IHubContext<SonarrHub, ISonarrHub> hubContext) 
            : base(hubContext) { }
        
        public Task HandleAsync(QueueUpdatedEvent message)
            => BroadcastResourceChange(ModelAction.Sync);
        
        public Task HandleAsync(PendingReleasesUpdatedEvent message)
            => BroadcastResourceChange(ModelAction.Sync);
    }
}
