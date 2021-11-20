using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Instrumentation.Extensions;

namespace NzbDrone.Core.Messaging.Commands
{
    public class TestCommandExecutor : IExecuteAsync<TestCommand>
    {
        private readonly ILogger<TestCommandExecutor> _logger;

        public TestCommandExecutor(ILogger<TestCommandExecutor> logger)
        {
            _logger = logger;
        }

        public Task ExecuteAsync(TestCommand message)
        {
            _logger.ProgressInfo("Starting Test command. duration {0}", message.Duration);
            Thread.Sleep(message.Duration);
            _logger.ProgressInfo("Completed Test command");
            return Task.CompletedTask;
        }
    }
}