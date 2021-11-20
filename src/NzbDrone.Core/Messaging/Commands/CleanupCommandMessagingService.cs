using System.Threading.Tasks;

namespace NzbDrone.Core.Messaging.Commands
{
    public class CleanupCommandMessagingService : IExecuteAsync<MessagingCleanupCommand>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public CleanupCommandMessagingService(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public Task ExecuteAsync(MessagingCleanupCommand message)
        {
            _commandQueueManager.CleanCommands();
            return Task.CompletedTask;
        }
    }
}
