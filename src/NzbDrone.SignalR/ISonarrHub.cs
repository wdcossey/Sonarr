using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NzbDrone.Common.Messaging;

namespace NzbDrone.SignalR
{
    public interface ISonarrHub
    {
        Task BroadcastMessage<TMessage>(TMessage message) where TMessage : SignalRMessage;

        Task BroadcastEvent(IEvent message);

        Task SendMessage(string str0, string str1);
    }


}
