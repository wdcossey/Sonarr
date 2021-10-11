using System.Threading.Tasks;

namespace NzbDrone.SignalR
{
    public interface ISonarrHub
    {
        Task BroadcastMessage<TMessage>(TMessage message) where TMessage : SignalRMessage;
    }
}
