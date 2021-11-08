using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Queue;

namespace Sonarr.Api.V3.Queue
{
    public class QueueStatusEventHandler : IHandle<QueueUpdatedEvent>, IHandle<PendingReleasesUpdatedEvent>
    {
        private readonly IQueueStatusDebounceWrapper _debounceWrapper;

        public QueueStatusEventHandler(IQueueStatusDebounceWrapper debounceWrapper)
            => _debounceWrapper = debounceWrapper;

        public void Handle(QueueUpdatedEvent message)
            => _debounceWrapper.Execute();
        
        public void Handle(PendingReleasesUpdatedEvent message)
            => _debounceWrapper.Execute();
    }
}
