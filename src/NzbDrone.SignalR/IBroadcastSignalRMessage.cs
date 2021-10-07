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

}
