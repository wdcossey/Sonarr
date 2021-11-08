using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Jobs;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.System.Tasks
{
    [ApiController]
    [SonarrApiRoute("system/task", RouteVersion.V3)]
    public class TaskController : ControllerBase
    {
        private readonly ITaskManager _taskManager;

        public TaskController(ITaskManager taskManager)
            => _taskManager = taskManager;

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
                return NotFound(); //TODO: Original returned `null`

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
    }
}
