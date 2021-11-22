using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using Timer = System.Timers.Timer;
using NzbDrone.Common.TPL;

namespace NzbDrone.Core.Jobs
{
    public class Scheduler : IHandleAsync<ApplicationStartedEvent>,
                             IHandleAsync<ApplicationShutdownRequested>
    {
        private readonly ITaskManager _taskManager;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly ILogger<Scheduler> _logger;
        private static readonly Timer Timer = new Timer();
        private static CancellationTokenSource _cancellationTokenSource;

        public Scheduler(ITaskManager taskManager, IManageCommandQueue commandQueueManager, ILogger<Scheduler> logger)
        {
            _taskManager = taskManager;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        private void ExecuteCommands()
        {
            try
            {
                Timer.Enabled = false;

                var tasks = _taskManager.GetPending().ToList();

                _logger.LogTrace("Pending Tasks: {Count}", tasks.Count);

                foreach (var task in tasks)
                {
                    _commandQueueManager.Push(task.TypeName, task.LastExecution, CommandPriority.Low, CommandTrigger.Scheduled);
                }
            }

            finally
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Timer.Enabled = true;
                }
            }
        }

        public Task HandleAsync(ApplicationStartedEvent message)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Timer.Interval = 1000 * 30;
            Timer.Elapsed += (o, args) => Task.Factory.StartNew(ExecuteCommands, _cancellationTokenSource.Token)
                .LogExceptions();

            Timer.Start();
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(ApplicationShutdownRequested message)
        {
            _logger.LogInformation("Shutting down scheduler");
            _cancellationTokenSource.Cancel(true);
            Timer.Stop();
            
            return Task.CompletedTask;
        }
    }
}