using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Sonarr.Blazor.Server.Hubs
{

    public interface ISonarrClient
    {
        Task ReceiveMessage(string user, string message);
    }

    public class SonarrHub: Hub<ISonarrClient>
    {

        public SonarrHub()
        {

        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }


        //[HubMethodName("SendMessageToUser")]
        public Task SendMessage(string user, string message)
        {
            return Clients.All.ReceiveMessage(user, message);
        }
    }
}
