using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
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

        [ProducesResponseType(typeof(IEnumerable<CommandResource>), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult GetStartedCommands()
            => Ok(_commandQueueManager.All().ToResource());

        [ProducesResponseType(typeof(IEnumerable<CommandResource>), StatusCodes.Status200OK)]
        [HttpGet("{id:int:required}")]
        public IActionResult GetCommand(int id)
            => Ok(_commandQueueManager.Get(id).ToResource());

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpDelete("{id:int:required}")]
        public IActionResult CancelCommand(int id)
        {
            _commandQueueManager.Cancel(id);
            return Ok(new object());
        }

        [ProducesResponseType(StatusCodes.Status201Created)]
        [HttpPost]
        public IActionResult StartCommand([FromBody] [ModelBinder(typeof(CommandResourceModelBinder))] CommandResource resource)
        {
            var trackedCommand = _commandQueueManager.Push(resource.Body, CommandPriority.Normal, CommandTrigger.Manual);
            return Created($"{Request.Path}/{trackedCommand.Id}", _commandQueueManager.Get(trackedCommand.Id).ToResource());
        }
    }
}
