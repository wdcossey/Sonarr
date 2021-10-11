﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ProgressMessaging;

namespace Sonarr.Api.V3.Commands
{
    [ApiController]
    [SonarrApiRoute("command", RouteVersion.V3)]
    //TODO: Remove `SonarrControllerBase`
    public class CommandController :
        ControllerBase,// SonarrRestModuleWithSignalR<CommandResource, CommandModel>
        IHandle<CommandUpdatedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly ICommandFactory _commandFactory;
        private readonly Debouncer _debouncer;
        private readonly Dictionary<int, CommandResource> _pendingUpdates;

        public CommandController(
                IManageCommandQueue commandQueueManager,
                ICommandFactory commandFactory/*,
                IBroadcastSignalRMessage signalRBroadcaster,*/) //TODO: SignalR
            //: base(signalRBroadcaster)
        {
            _commandQueueManager = commandQueueManager;
            _commandFactory = commandFactory;

            _debouncer = new Debouncer(SendUpdates, TimeSpan.FromSeconds(0.1));
            _pendingUpdates = new Dictionary<int, CommandResource>();

            /*GetResourceById = GetCommand;
            CreateResource = StartCommand;
            GetResourceAll = GetStartedCommands;
            DeleteResource = CancelCommand;

            PostValidator.RuleFor(c => c.Name).NotBlank();*/
        }

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
        public IActionResult StartCommand([FromBody] CommandResource resource)
        {
            var command = _commandFactory.Create(resource!.Name);
            command!.Trigger = CommandTrigger.Manual;
            command!.SuppressMessages = !resource!.SendUpdatesToClient;
            command!.SendUpdatesToClient = true;

            if (Request.Headers.TryGetValue("User-Agent", out var userAgent))
                command!.ClientUserAgent = userAgent;

            var trackedCommand = _commandQueueManager.Push(command, CommandPriority.Normal, CommandTrigger.Manual);

            return Created($"{Request.Path}/{trackedCommand.Id}", _commandQueueManager.Get(trackedCommand.Id).ToResource());
        }

        public void Handle(CommandUpdatedEvent message)
        {
            if (!message.Command.Body.SendUpdatesToClient)
                return;

            lock (_pendingUpdates)
            {
                _pendingUpdates[message.Command.Id] = message.Command.ToResource();
            }

            _debouncer.Execute();
        }

        private void SendUpdates()
        {
            //TODO
            lock (_pendingUpdates)
            {
                var pendingUpdates = _pendingUpdates.Values.ToArray();
                _pendingUpdates.Clear();

                foreach (var pendingUpdate in pendingUpdates)
                {
                    /*BroadcastResourceChange(ModelAction.Updated, pendingUpdate);

                    if (pendingUpdate.Name == typeof(MessagingCleanupCommand).Name.Replace("Command", "") &&
                        pendingUpdate.Status == CommandStatus.Completed)
                    {
                        BroadcastResourceChange(ModelAction.Sync);
                    }*/
                }
            }
        }
    }
}
