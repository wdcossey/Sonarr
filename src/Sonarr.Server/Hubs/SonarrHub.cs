using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NzbDrone.Common.Messaging;
//using NzbDrone.Core.Messaging.Events;
//using NzbDrone.SignalR;

namespace Sonarr.Server.Hubs
{

    public class SonarrHub: Hub//<ISonarrHub>
    {
        /*private readonly IBroadcastMessageEventWrapper _eventWrapper;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //_eventWrapper.MessageEventHandler -= OnEventHandler;
            }

            base.Dispose(disposing);
        }

        public SonarrHub(IBroadcastMessageEventWrapper eventWrapper)
        {
            _eventWrapper = eventWrapper;
            _eventWrapper.MessageEventHandler += OnEventHandler;
        }

        private void OnEventHandler(IEvent @event)
        {
            try
            {
                Clients.All.BroadcastEvent(@event);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        //[HubMethodName("SendMessageToUser")]
        public Task BroadcastMessage(SignalRMessage message)
            => Clients.All.BroadcastMessage(message);*/

        //[HubMethodName("SendMessageToUser")]
        public Task SendMessage(string hello, string world)
            => Clients.All.SendCoreAsync(nameof(SendMessage), new []{ hello, world });
    }
}
