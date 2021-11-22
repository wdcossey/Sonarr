using System.Threading.Tasks;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Queue;

namespace Sonarr.Api.V3.Queue
{
    public class QueueStatusEventHandler : IHandleAsync<QueueUpdatedEvent>, IHandleAsync<PendingReleasesUpdatedEvent>
    {
        private readonly IQueueStatusDebounceWrapper _debounceWrapper;

        public QueueStatusEventHandler(IQueueStatusDebounceWrapper debounceWrapper)
            => _debounceWrapper = debounceWrapper;

        public Task HandleAsync(QueueUpdatedEvent message)
        {
            _debounceWrapper.Execute();
            return Task.CompletedTask;
        }

        public Task HandleAsync(PendingReleasesUpdatedEvent message)
        {
            _debounceWrapper.Execute();
            return Task.CompletedTask;
        }
    }
}
