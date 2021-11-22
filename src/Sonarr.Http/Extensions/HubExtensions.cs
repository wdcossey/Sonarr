using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.SignalR;
using NzbDrone.SignalR.Extensions;
using Sonarr.Http;
using Sonarr.Http.REST;

namespace NzbDrone.Http.Extensions
{
    public static class HubExtensions
    {
        public static Task BroadcastResourceChange<TResource>(this IHubContext<SonarrHub, ISonarrHub> context, ModelAction action, TResource resource, string name)
            where TResource : RestResource
        {
            var signalRMessage = new SignalRMessage
            {
                Name = name,
                Body = new ResourceChangeMessage<TResource>(resource, action),
                Action = action
            };

            return context?.BroadcastMessage(signalRMessage);
        }
        
        public static Task BroadcastResourceChange<TResource>(this IHubContext<SonarrHub, ISonarrHub> context, ModelAction action, TResource resource)
            where TResource : RestResource
            => context?.BroadcastResourceChange(action, resource, resource.GetBroadcastName());

        public static Task BroadcastResourceChange<TResource>(this IHubContext<SonarrHub, ISonarrHub> context, ModelAction action, string name)
            where TResource : RestResource
        {
            var signalRMessage = new SignalRMessage
            {
                Name = name,
                Body = new ResourceChangeMessage<TResource>(action),
                Action = action
            };

            return context?.BroadcastMessage(signalRMessage);
        }

        public static Task BroadcastResourceChange<TResource>(this IHubContext<SonarrHub, ISonarrHub> context, ModelAction action)
            where TResource : RestResource
            => context?.BroadcastResourceChange<TResource>(action, typeof(TResource).GetBroadcastName());

        public static Task BroadcastResourceChange<TResource>(this IHubContext<SonarrHub, ISonarrHub> context, ModelAction action, int id, Func<int, TResource> getResourceByIdFunc = null, Func<TResource, string> getNameFunc = null)
            where TResource : RestResource, new() 
        {
            if (action == ModelAction.Deleted)
            {
                var resource = new TResource {Id = id};
                return context?.BroadcastResourceChange(action, resource, getNameFunc?.Invoke(resource) ?? resource.GetBroadcastName());
            }
            else
            {
                var resource = getResourceByIdFunc?.Invoke(id) ?? throw new ArgumentNullException(nameof(getResourceByIdFunc));
                return context?.BroadcastResourceChange(action, resource, getNameFunc?.Invoke(resource) ?? resource.GetBroadcastName());
            }
        }
        
        public static Task BroadcastResourceChange<TResource>(this IHubContext<SonarrHub, ISonarrHub> context, ModelAction action, int id, Func<int, TResource> getResourceByIdFunc = null)
            where TResource : RestResource, new()
            => context?.BroadcastResourceChange(action, id, getResourceByIdFunc, resource => resource.GetBroadcastName());
    }
}
