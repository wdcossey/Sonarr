using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ProgressMessaging;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.Commands
{
    public class CommandEventHandler : EventHandlerBase<CommandResource, CommandModel>,
                                       IHandle<CommandUpdatedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Debouncer _debouncer;
        private readonly Dictionary<int, CommandResource> _pendingUpdates;

        public CommandEventHandler(IManageCommandQueue commandQueueManager, IHubContext<SonarrHub, ISonarrHub> hubContext)
            : base(hubContext)
        {
            _commandQueueManager = commandQueueManager;
            _debouncer = new Debouncer(SendUpdates, TimeSpan.FromSeconds(0.1));
            _pendingUpdates = new Dictionary<int, CommandResource>();
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
            lock (_pendingUpdates)
            {
                var pendingUpdates = _pendingUpdates.Values.ToArray();
                _pendingUpdates.Clear();

                foreach (var pendingUpdate in pendingUpdates)
                {
                    BroadcastResourceChange(ModelAction.Updated, pendingUpdate);

                    if (pendingUpdate.Name != nameof(MessagingCleanupCommand).Replace("Command", "") ||
                        pendingUpdate.Status != CommandStatus.Completed)
                        continue;

                    BroadcastResourceChange(ModelAction.Sync);
                }
            }
        }

        protected override CommandResource GetResourceById(int id)
            => _commandQueueManager.Get(id).ToResource();
    }
}
