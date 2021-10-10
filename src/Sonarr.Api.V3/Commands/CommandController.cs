using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ProgressMessaging;
using IServiceProvider = System.IServiceProvider;

namespace Sonarr.Api.V3.Commands
{
    [ApiController]
    [Route("/api/v3/command")]
    public class CommandController : SonarrControllerBase<CommandResource, CommandModel>,// SonarrRestModuleWithSignalR<CommandResource, CommandModel>
                                                   IHandle<CommandUpdatedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly Debouncer _debouncer;
        private readonly Dictionary<int, CommandResource> _pendingUpdates;

        public CommandController(IManageCommandQueue commandQueueManager,
                //IBroadcastSignalRMessage signalRBroadcaster,
                             IServiceProvider serviceProvider)
            //: base(signalRBroadcaster)
        {
            _commandQueueManager = commandQueueManager;
            _serviceProvider = serviceProvider;

            _debouncer = new Debouncer(SendUpdates, TimeSpan.FromSeconds(0.1));
            _pendingUpdates = new Dictionary<int, CommandResource>();

            /*GetResourceById = GetCommand;
            CreateResource = StartCommand;
            GetResourceAll = GetStartedCommands;
            DeleteResource = CancelCommand;

            PostValidator.RuleFor(c => c.Name).NotBlank();*/
        }

        protected override Task<IList<CommandResource>> GetAllResourcesAsync()
            => Task.FromResult<IList<CommandResource>>(_commandQueueManager.All().ToResource());

        protected override Task<CommandResource> GetResourceByIdAsync(int id)
            => Task.FromResult(_commandQueueManager.Get(id).ToResource());

        protected override Task DeleteResourceByIdAsync(int id)
        {
            _commandQueueManager.Cancel(id);
            return Task.CompletedTask;
        }

        protected override Task<CommandResource> UpdateResourceAsync(CommandResource resource)
            => throw new NotImplementedException();

        protected override async Task<CommandResource> CreateResourceAsync(CommandResource resource)
        {

            //TODO: Old implementation got the `Type` not an instance
            var commandType = //Type.GetType($"{commandResource.Name}{nameof(Command)}");
                _serviceProvider.GetServices<Command>()
                    .Single(c => c.Name.Replace("Command", "")
                        .Equals(resource!.Name, StringComparison.InvariantCultureIgnoreCase)).GetType();

            dynamic command = Activator.CreateInstance(commandType);
            command!.Trigger = CommandTrigger.Manual;
            command!.SuppressMessages = !resource!.SendUpdatesToClient;
            command!.SendUpdatesToClient = true;

            if (Request.Headers.TryGetValue("User-Agent", out var userAgent))
                command!.ClientUserAgent = userAgent;

            var trackedCommand = _commandQueueManager.Push(command, CommandPriority.Normal, CommandTrigger.Manual);

            return await GetResourceByIdAsync((int)trackedCommand.Id);
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
