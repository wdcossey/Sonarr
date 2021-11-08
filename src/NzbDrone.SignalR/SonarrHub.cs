using Microsoft.AspNetCore.SignalR;

namespace NzbDrone.SignalR
{
    public class SonarrHub: Hub<ISonarrHub>
    {
        /*//[HubMethodName("SendMessageToUser")]
        public Task BroadcastMessage(SignalRMessage message)
            => Clients.All.BroadcastMessage(message);

        //[HubMethodName("SendMessageToUser")]
        public Task SendMessage(string hello, string world)
            => Clients.All.BroadcastEvent();*/
    }
}
