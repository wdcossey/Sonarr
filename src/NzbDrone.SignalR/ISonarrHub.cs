using System.Threading.Tasks;
using NzbDrone.Common.Messaging;

namespace NzbDrone.SignalR
{
    public interface ISonarrHub
    {
        Task BroadcastMessage<TMessage>(TMessage message) 
            where TMessage : SignalRMessage;

        Task BroadcastEvent<TEvent>(TEvent @event)
            where TEvent : IEvent;
    }
}
