using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NzbDrone.SignalR;

namespace Sonarr.Blazor.Server.Hubs
{
    public class SonarrHub: Hub<ISonarrHub>
    {

        public SonarrHub()
        {

        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        //[HubMethodName("SendMessageToUser")]
        public Task BroadcastMessage(SignalRMessage message)
            => Clients.All.BroadcastMessage(message);
    }
}
