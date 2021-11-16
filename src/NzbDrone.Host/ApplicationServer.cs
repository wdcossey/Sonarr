using System;
//using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Composition;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;
//using NzbDrone.Host.WebHost;

namespace NzbDrone.Host
{
    public interface INzbDroneServiceFactory
    {
        //ServiceBase Build();
        void Start();
    }

    public class NzbDroneServiceFactory : /*ServiceBase, */INzbDroneServiceFactory, IHandle<ApplicationShutdownRequested>
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IRuntimeInfo _runtimeInfo;
        //private readonly IHostController _hostController; //TODO: Legacy IHostController
        private readonly IStartupContext _startupContext;
        private readonly IBrowserService _browserService;
        //private readonly IContainer _container; //TODO: Legacy IContainer
        private readonly ILogger<NzbDroneServiceFactory> _logger;

        public NzbDroneServiceFactory(IConfigFileProvider configFileProvider,
                                      //IHostController hostController, //TODO: Legacy IHostController
                                      IRuntimeInfo runtimeInfo,
                                      IStartupContext startupContext,
                                      IBrowserService browserService,
                                      //IContainer container, //TODO: Legacy IContainer
                                      ILogger<NzbDroneServiceFactory> logger)
        {
            _configFileProvider = configFileProvider;
            //_hostController = hostController; //TODO: Legacy IHostController
            _runtimeInfo = runtimeInfo;
            _startupContext = startupContext;
            _browserService = browserService;
            //_container = container;
            _logger = logger;
        }

        /*protected override void OnStart(string[] args)
        {
            Start();
        }*/

        public void Start()
        {
            if (OsInfo.IsNotWindows)
            {
                //Console.CancelKeyPress += (sender, eventArgs) => LogManager.Configuration = null;
            }

            _runtimeInfo.IsExiting = false;
            //DbFactory.RegisterDatabase(_container);

            //_container.Resolve<IEventAggregator>().PublishEvent(new ApplicationStartingEvent());

            if (_runtimeInfo.IsExiting)
            {
                return;
            }

            //_hostController.StartServer();

            if (!_startupContext.Flags.Contains(StartupContext.NO_BROWSER)
                && _configFileProvider.LaunchBrowser)
            {
                _browserService.LaunchWebUI();
            }

            //_container.Resolve<IEventAggregator>().PublishEvent(new ApplicationStartedEvent());
        }

        /*protected override void OnStop()
        {
            Shutdown();
        }*/

        /*public ServiceBase Build()
        {
            return this;
        }*/

        private void Shutdown()
        {
            _logger.LogInformation("Attempting to stop application.");
            //_hostController.StopServer();
            _logger.LogInformation("Application has finished stop routine.");
            _runtimeInfo.IsExiting = true;
        }

        public void Handle(ApplicationShutdownRequested message)
        {
            if (!_runtimeInfo.IsWindowsService)
            {
                if (message.Restarting)
                {
                    _runtimeInfo.RestartPending = true;
                }

                //LogManager.Configuration = null;
                Shutdown();
            }
        }
    }
}
