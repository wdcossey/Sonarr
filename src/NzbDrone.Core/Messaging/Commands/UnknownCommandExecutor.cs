using Microsoft.Extensions.Logging;

namespace NzbDrone.Core.Messaging.Commands
{
    public class UnknownCommandExecutor : IExecute<UnknownCommand>
    {
        private readonly ILogger<UnknownCommandExecutor> _logger;

        public UnknownCommandExecutor(ILogger<UnknownCommandExecutor> logger)
            => _logger = logger;

        public void Execute(UnknownCommand message)
            => _logger.LogDebug("Ignoring unknown command {ContractName}", message.ContractName);
    }
}
