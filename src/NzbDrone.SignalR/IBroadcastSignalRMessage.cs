using System;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.SignalR
{
    public interface IBroadcastSignalRMessage
    {
        bool IsConnected { get; }
        void BroadcastMessage(SignalRMessage message);
    }

    public sealed class NzbDronePersistentConnection : IBroadcastSignalRMessage
    {
        public bool IsConnected { get; } = false;

        public void BroadcastMessage(SignalRMessage message)
        {
            throw new System.NotImplementedException();
        }
    }

    public class BroadcastMessageEvent : IEvent
    {
        public SignalRMessage Message { get; set; }
    }

    public interface IBroadcastMessageEventWrapper : IHandleAsync<IEvent>
    {
        delegate void BroadcastMessageEventHandler(IEvent e);

        event BroadcastMessageEventHandler MessageEventHandler;
    }

    public class BroadcastMessageEventWrapper : IBroadcastMessageEventWrapper
    {
        public void HandleAsync(IEvent message)
            => MessageEventHandler?.Invoke(message);

        public event IBroadcastMessageEventWrapper.BroadcastMessageEventHandler MessageEventHandler;
    }
}
