using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.SignalR
{
    [Authorize(Roles = "sonarr-signalr")]
    public class SonarrHub: Hub<ISonarrHub>
    {
        /// <summary>
        /// /hubs/sonarr
        /// </summary>
        public const string RoutePattern = "/hubs/sonarr";
        
        private readonly ILogger<SonarrHub> _logger;

        public SonarrHub(ILogger<SonarrHub> logger)
            => _logger = logger;

        /*//[HubMethodName("SendMessageToUser")]
        public Task BroadcastMessage(SignalRMessage message)
            => Clients.All.BroadcastMessage(message);*/

        /*//[HubMethodName("SendMessageToUser")]
        public Task SendMessage(string hello, string world)
            => Clients.All.BroadcastEvent();*/

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected. Identity: {IdentityName}; ConnectionId: {ConnectionId}; IpAddress: {RemoteIpAddress}:{RemotePort}", Context.User.Identity.Name, Context.ConnectionId, Context.GetHttpContext().Connection.RemoteIpAddress.MapToIPv4(), Context.GetHttpContext().Connection.RemotePort);
            SendVersion();
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Client disconnected. Identity: {IdentityName}; ConnectionId: {ConnectionId}; IpAddress: {RemoteIpAddress}:{RemotePort}", Context.User.Identity.Name, Context.ConnectionId, Context.GetHttpContext().Connection.RemoteIpAddress.MapToIPv4(), Context.GetHttpContext().Connection.RemotePort);
            return base.OnDisconnectedAsync(exception);
        }

        private Task SendVersion()
        {
            return Clients.Caller.BroadcastMessage(new SignalRMessage
            {
                Name = "Version",
                Body = new
                {
                    Version = BuildInfo.Version.ToString()
                }
            });
        }  
    }
}
