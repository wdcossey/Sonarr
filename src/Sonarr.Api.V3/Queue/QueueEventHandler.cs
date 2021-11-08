using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Queue;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.Queue
{
    public class QueueEventHandler: EventHandlerBase<QueueResource>, IHandle<QueueUpdatedEvent>, IHandle<PendingReleasesUpdatedEvent>
    {
        public QueueEventHandler(IHubContext<SonarrHub, ISonarrHub> hubContext) 
            : base(hubContext) { }
        
        public void Handle(QueueUpdatedEvent message)
            => BroadcastResourceChange(ModelAction.Sync);
        
        public void Handle(PendingReleasesUpdatedEvent message)
            => BroadcastResourceChange(ModelAction.Sync);
    }
}
