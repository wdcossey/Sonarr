namespace NzbDrone.Host.WebHost
{
    public interface IHostController
    {
        void StartServer();
        void StopServer();
    }
}
