using System;
using Microsoft.Extensions.Logging;
using NzbDrone.Common;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Processes;
using NzbDrone.Host.AccessControl;
//using IServiceProvider = NzbDrone.Common.IServiceProvider;

namespace NzbDrone.Host
{
    public class Router
    {
        private readonly INzbDroneServiceFactory _nzbDroneServiceFactory;
        //private readonly IServiceProvider _serviceProvider; //TODO: Legacy IServiceProvider
        private readonly IConsoleService _consoleService;
        private readonly IRuntimeInfo _runtimeInfo;
        private readonly IProcessProvider _processProvider;
        private readonly IRemoteAccessAdapter _remoteAccessAdapter;
        private readonly ILogger<Router> _logger;

        public Router(INzbDroneServiceFactory nzbDroneServiceFactory,
                      //IServiceProvider serviceProvider, //TODO: Legacy IServiceProvider
                      IConsoleService consoleService,
                      IRuntimeInfo runtimeInfo,
                      IProcessProvider processProvider,
                      IRemoteAccessAdapter remoteAccessAdapter,
                      ILogger<Router> logger)
        {
            _nzbDroneServiceFactory = nzbDroneServiceFactory;
            //_serviceProvider = serviceProvider; //TODO: Legacy IServiceProvider
            _consoleService = consoleService;
            _runtimeInfo = runtimeInfo;
            _processProvider = processProvider;
            _remoteAccessAdapter = remoteAccessAdapter;
            _logger = logger;
        }

        public void Route(ApplicationModes applicationModes)
        {
            _logger.LogInformation("Application mode: {0}", applicationModes);

            switch (applicationModes)
            {
                case ApplicationModes.Service:
                    {
                        _logger.LogDebug("Service selected");

                        throw new NotImplementedException();
                        //_serviceProvider.Run(_nzbDroneServiceFactory.Build());

                        break;
                    }

                case ApplicationModes.Interactive:
                    {
                        _logger.LogDebug("{Message}", _runtimeInfo.IsWindowsTray ? "Tray selected" : "Console selected");
                        _nzbDroneServiceFactory.Start();

                        break;
                    }
                case ApplicationModes.InstallService:
                    {
                        _logger.LogDebug("Install Service selected");
                        throw new NotImplementedException();
                        /*if (_serviceProvider.ServiceExist(ServiceProvider.SERVICE_NAME))
                        {
                            _consoleService.PrintServiceAlreadyExist();
                        }
                        else
                        {
                            _remoteAccessAdapter.MakeAccessible(true);
                            _serviceProvider.Install(ServiceProvider.SERVICE_NAME);
                            _serviceProvider.SetPermissions(ServiceProvider.SERVICE_NAME);

                            // Start the service and exit.
                            // Ensures that there isn't an instance of Sonarr already running that the service account cannot stop.
                            _processProvider.SpawnNewProcess("sc.exe", $"start {ServiceProvider.SERVICE_NAME}", null, true);
                        }*/
                        break;
                    }
                case ApplicationModes.UninstallService:
                    {
                        _logger.LogDebug("Uninstall Service selected");
                        throw new NotImplementedException();
                        /*if (!_serviceProvider.ServiceExist(ServiceProvider.SERVICE_NAME))
                        {
                            _consoleService.PrintServiceDoesNotExist();
                        }
                        else
                        {
                            _serviceProvider.Uninstall(ServiceProvider.SERVICE_NAME);
                        }*/

                        break;
                    }
                case ApplicationModes.RegisterUrl:
                    {
                        _logger.LogDebug("Regiser URL selected");
                        _remoteAccessAdapter.MakeAccessible(false);

                        break;
                    }
                default:
                    {
                        _consoleService.PrintHelp();
                        break;
                    }
            }
        }
    }
}
