using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sonarr.Http.Extensions;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ProgressMessaging;
using NzbDrone.SignalR;
using Sonarr.Http;
using Sonarr.Http.Validation;
using IServiceProvider = System.IServiceProvider;


namespace NzbDrone.Api.Commands
{
    public class CommandModule : SonarrRestModuleWithSignalR<CommandResource, CommandModel>, IHandle<CommandUpdatedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly Debouncer _debouncer;
        private readonly Dictionary<int, CommandResource> _pendingUpdates;

        public CommandModule(IManageCommandQueue commandQueueManager,
                             IBroadcastSignalRMessage signalRBroadcaster,
                             IServiceProvider serviceProvider)
            : base(signalRBroadcaster)
        {
            _commandQueueManager = commandQueueManager;
            _serviceProvider = serviceProvider;

            GetResourceById = GetCommand;
            CreateResource = StartCommand;
            GetResourceAll = GetStartedCommands;

            PostValidator.RuleFor(c => c.Name).NotBlank();

            _debouncer = new Debouncer(SendUpdates, TimeSpan.FromSeconds(0.1));
            _pendingUpdates = new Dictionary<int, CommandResource>();

        }

        private CommandResource GetCommand(int id)
        {
            return _commandQueueManager.Get(id).ToResource();
        }

        private int StartCommand(CommandResource commandResource)
        {
            //TODO: Old implementation got the `Type` not an instance
            var commandType = _serviceProvider.GetServices<Command>()
                                             .Single(c => c.Name.Replace("Command", "").Equals(commandResource.Name, StringComparison.InvariantCultureIgnoreCase));

            dynamic command = Request.Body.FromJson(commandType.GetType());
            command.Trigger = CommandTrigger.Manual;

            var trackedCommand = _commandQueueManager.Push(command, CommandPriority.Normal, CommandTrigger.Manual);
            return trackedCommand.Id;
        }

        private List<CommandResource> GetStartedCommands()
        {
            return _commandQueueManager.GetStarted().ToResource();
        }

        public void Handle(CommandUpdatedEvent message)
        {
            if (message.Command.Body.SendUpdatesToClient)
            {
                lock (_pendingUpdates)
                {
                    _pendingUpdates[message.Command.Id] = message.Command.ToResource();
                }
                _debouncer.Execute();
            }
        }

        private void SendUpdates()
        {
            lock (_pendingUpdates)
            {
                var pendingUpdates = _pendingUpdates.Values.ToArray();
                _pendingUpdates.Clear();

                foreach (var pendingUpdate in pendingUpdates)
                {
                    BroadcastResourceChange(ModelAction.Updated, pendingUpdate);
                }
            }
        }
    }
}
