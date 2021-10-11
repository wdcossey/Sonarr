using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.System.Tasks
{
    [ApiController]
    [SonarrApiRoute("system/task", RouteVersion.V3)]
    public class TaskController : ControllerBase, IHandle<CommandExecutedEvent> //<TaskResource, ScheduledTask>
    {
        private readonly ITaskManager _taskManager;

        public TaskController(
            ITaskManager taskManager/*,
            IBroadcastSignalRMessage broadcastSignalRMessage*/) //TODO: SignalR Hub
        {
            _taskManager = taskManager;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var result = _taskManager
                .GetAll()
                .Select(ConvertToResource)
                .OrderBy(t => t.Name);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetTask(int id)
        {
            var task = _taskManager
                .GetAll()
                .SingleOrDefault(t => t.Id == id);

            if (task == null)
                return NotFound();

            return Ok(ConvertToResource(task));
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

        public void Handle(CommandExecutedEvent message)
        {
            //TODO: SignalR Hub
            //BroadcastResourceChange(ModelAction.Sync);
        }
    }
}
