using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.System.Tasks
{
    public class TaskEventHandler : EventHandlerBase<TaskResource, ScheduledTask>, IHandleAsync<CommandExecutedEvent>
    {
        private readonly ITaskManager _taskManager;

        public TaskEventHandler(IHubContext<SonarrHub, ISonarrHub> hubContext, ITaskManager taskManager) 
            : base(hubContext) => (_taskManager) = (taskManager);
        
        public Task HandleAsync(CommandExecutedEvent message)
            => BroadcastResourceChange(ModelAction.Sync);

        protected override TaskResource GetResourceById(int id)
        {
            var task = _taskManager
                .GetAll()
                .SingleOrDefault(t => t.Id == id);
            
            if (task == null)
                return null;

            return ConvertToResource(task);
        }
        
        private static TaskResource ConvertToResource(ScheduledTask scheduledTask)
        {
            var taskName = scheduledTask.TypeName.Split('.').Last().Replace("Command", "");

            return new TaskResource
            {
                Id = scheduledTask.Id,
                Name = taskName.SplitCamelCase(),
                TaskName = taskName,
                Interval = scheduledTask.Interval,
                LastExecution = scheduledTask.LastExecution,
                NextExecution = scheduledTask.LastExecution.AddMinutes(scheduledTask.Interval)
            };
        }
    }
}
