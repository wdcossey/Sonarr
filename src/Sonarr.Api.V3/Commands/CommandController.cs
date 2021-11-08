using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Messaging.Commands;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Commands
{
    [ApiController]
    [SonarrApiRoute("command", RouteVersion.V3)]
    public class CommandController : ControllerBase
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public CommandController(IManageCommandQueue commandQueueManager)
            => _commandQueueManager = commandQueueManager;

        [HttpGet]
        public IActionResult GetStartedCommands()
            => Ok(_commandQueueManager.All().ToResource());

        [HttpGet("{id:int:required}")]
        public IActionResult GetCommand(int id)
            => Ok(_commandQueueManager.Get(id).ToResource());

        [HttpDelete("{id:int:required}")]
        public IActionResult CancelCommand(int id)
        {
            _commandQueueManager.Cancel(id);
            return Ok(new object());
        }

        [HttpPost]
        public IActionResult StartCommand([FromBody] [ModelBinder(typeof(CommandResourceModelBinder))] CommandResource resource)
        {
            var trackedCommand = _commandQueueManager.Push(resource.Body, CommandPriority.Normal, CommandTrigger.Manual);
            return Created($"{Request.Path}/{trackedCommand.Id}", _commandQueueManager.Get(trackedCommand.Id).ToResource());
        }
    }
}
