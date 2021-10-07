using System;
using System.Text.Json;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebSockets;
using Microsoft.AspNetCore.Http;
using Sonarr.Api.V3.Series;

namespace NzbDrone.Host.WebHost
{
    public class WebSocketsServer: WebSocketModule
    {
        public WebSocketsServer()
            : base("/signalr", true)
        {
            // placeholder

            //AddProtocol("json");
        }

        protected override Task OnFrameReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
        {
            return base.OnFrameReceivedAsync(context, buffer, result);
        }

        [Route(HttpVerbs.Get, "/negotiate")]
        public void Negotiate()
        {
            return;
        }

        /// <inheritdoc />
        protected override Task OnMessageReceivedAsync(
            IWebSocketContext context,
            byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
            => SendToOthersAsync(context, Encoding.GetString(rxBuffer));

        /// <inheritdoc />
        protected override Task OnClientConnectedAsync(IWebSocketContext context)
            => Task.WhenAll(
                SendAsync(context, "Welcome to the chat room!"),
                SendToOthersAsync(context, "Someone joined the chat room."));

        /// <inheritdoc />
        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
            => SendToOthersAsync(context, "Someone left the chat room.");

        private Task SendToOthersAsync(IWebSocketContext context, string payload)
            => BroadcastAsync(payload, c => c != context);
    }
}
