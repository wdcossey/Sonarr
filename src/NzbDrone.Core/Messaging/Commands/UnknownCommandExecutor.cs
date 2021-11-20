using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NzbDrone.Core.Messaging.Commands
{
    public class UnknownCommandExecutor : IExecuteAsync<UnknownCommand>
    {
        private readonly ILogger<UnknownCommandExecutor> _logger;

        public UnknownCommandExecutor(ILogger<UnknownCommandExecutor> logger)
            => _logger = logger;

        public Task ExecuteAsync(UnknownCommand message)
        {
            _logger.LogDebug("Ignoring unknown command {ContractName}", message.ContractName);
            return Task.CompletedTask;
        }
    }
}
