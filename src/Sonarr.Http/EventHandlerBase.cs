using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Http.Extensions;
using NzbDrone.SignalR;
using Sonarr.Http.REST;

namespace Sonarr.Http
{
    public abstract class EventHandlerBase<TResource>
        where TResource : RestResource, new()
    {
        protected IHubContext<SonarrHub, ISonarrHub> HubContext { get; }

        protected EventHandlerBase(IHubContext<SonarrHub, ISonarrHub> hubContext)
            => HubContext = hubContext;
        
        protected void BroadcastResourceChange(ModelAction action)
        {
            if (GetType().Namespace?.Contains("V3") != true)
                return;

            var broadcastName = this.GetType().GetBroadcastName() ?? typeof(TResource).GetBroadcastName();

            HubContext?.BroadcastResourceChange<TResource>(action, broadcastName);
        }

        protected void BroadcastResourceChange(ModelAction action, TResource resource)
        {
            if (GetType().Namespace?.Contains("V3") != true)
                return;

            var broadcastName = this.GetType().GetBroadcastName() ?? resource.GetBroadcastName();
            HubContext?.BroadcastResourceChange(action, resource, broadcastName);
        }
    }

    public abstract class EventHandlerBase<TResource, TModel> : EventHandlerBase<TResource>, IHandle<ModelEvent<TModel>>
        where TResource : RestResource, new()
        where TModel : ModelBase, new()
    {
        protected EventHandlerBase(IHubContext<SonarrHub, ISonarrHub> hubContext)
            : base(hubContext) { }

        public void Handle(ModelEvent<TModel> message)
        {
            if (message.Action is ModelAction.Deleted or ModelAction.Sync)
                BroadcastResourceChange(message.Action);

            BroadcastResourceChange(message.Action, message.ModelId);
        }

        protected abstract TResource GetResourceById(int id);

        private void BroadcastResourceChange(ModelAction action, int id)
        {
            if (action == ModelAction.Deleted)
                BroadcastResourceChange(action, new TResource {Id = id});

            BroadcastResourceChange(action, GetResourceById(id));
        }
    }
}
