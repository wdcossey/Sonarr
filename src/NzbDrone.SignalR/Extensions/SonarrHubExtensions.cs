using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NzbDrone.Common.Messaging;

namespace NzbDrone.SignalR.Extensions
{
    public static class SonarrHubExtensions
    {
        public static Task BroadcastMessage<TMessage>(this IHubContext<SonarrHub, ISonarrHub> context, TMessage message)
            where TMessage : SignalRMessage
            => context?.Clients?.All.BroadcastMessage(message);
        
        public static Task BroadcastEvent<TEvent>(this IHubContext<SonarrHub, ISonarrHub> context, TEvent @event)
            where TEvent : IEvent
            => context?.Clients?.All.BroadcastEvent(@event);
    }
}
