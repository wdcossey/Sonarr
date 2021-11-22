using System.Linq;
using System.Threading.Tasks;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv.Commands;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.Tv
{
    public class SeriesAddedHandler : IHandleAsync<SeriesAddedEvent>,
                                      IHandleAsync<SeriesImportedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public SeriesAddedHandler(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public Task HandleAsync(SeriesAddedEvent message)
        {
            _commandQueueManager.Push(new RefreshSeriesCommand(message.Series.Id, true));
            return Task.CompletedTask;
        }

        public Task HandleAsync(SeriesImportedEvent message)
        {
            _commandQueueManager.PushMany(message.SeriesIds.Select(s => new RefreshSeriesCommand(s, true)).ToList());
            return Task.CompletedTask;
        }
    }
}
