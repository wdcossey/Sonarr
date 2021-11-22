using System.Threading.Tasks;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListUpdatedHandler : IHandleAsync<ProviderUpdatedEvent<IImportList>>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public ImportListUpdatedHandler(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public Task HandleAsync(ProviderUpdatedEvent<IImportList> message)
        {
            _commandQueueManager.Push(new ImportListSyncCommand(message.Definition.Id));
            return Task.CompletedTask;
        }
    }
}
