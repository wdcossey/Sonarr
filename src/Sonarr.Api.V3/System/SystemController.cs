using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nancy.Routing;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Lifecycle;

namespace Sonarr.Api.V3.System
{
    [ApiController]
    [Route("/api/v3/system")]
    public class SystemController : ControllerBase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IRuntimeInfo _runtimeInfo;
        private readonly IPlatformInfo _platformInfo;
        private readonly IOsInfo _osInfo;
        //private readonly IRouteCacheProvider _routeCacheProvider;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IMainDatabase _database;
        private readonly ILifecycleService _lifecycleService;
        private readonly IDeploymentInfoProvider _deploymentInfoProvider;

        public SystemController(
                IAppFolderInfo appFolderInfo,
                IRuntimeInfo runtimeInfo,
                IPlatformInfo platformInfo,
                IOsInfo osInfo,
                //IRouteCacheProvider routeCacheProvider,
                IConfigFileProvider configFileProvider,
                IMainDatabase database,
                ILifecycleService lifecycleService,
                IDeploymentInfoProvider deploymentInfoProvider)
            //: base("system")
        {
            _appFolderInfo = appFolderInfo;
            _runtimeInfo = runtimeInfo;
            _platformInfo = platformInfo;
            _osInfo = osInfo;
            //_routeCacheProvider = routeCacheProvider;
            _configFileProvider = configFileProvider;
            _database = database;
            _lifecycleService = lifecycleService;
            _deploymentInfoProvider = deploymentInfoProvider;
            /*Get("/status",  x => GetStatus());
            Get("/routes",  x => GetRoutes());
            Post("/shutdown",  x => Shutdown());
            Post("/restart",  x => Restart());*/
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            //TODO: Create converter for `Version`
            var value = new
                   {
                       Version = BuildInfo.Version.ToString(),
                       BuildTime = BuildInfo.BuildDateTime,
                       IsDebug = BuildInfo.IsDebug,
                       IsProduction = RuntimeInfo.IsProduction,
                       IsAdmin = _runtimeInfo.IsAdmin,
                       IsUserInteractive = RuntimeInfo.IsUserInteractive,
                       StartupPath = _appFolderInfo.StartUpFolder,
                       AppData = _appFolderInfo.GetAppDataPath(),
                       OsName = _osInfo.Name,
                       OsVersion = _osInfo.Version,
                       IsMonoRuntime = PlatformInfo.IsMono,
                       IsMono = PlatformInfo.IsMono,
                       IsLinux = OsInfo.IsLinux,
                       IsOsx = OsInfo.IsOsx,
                       IsWindows = OsInfo.IsWindows,
                       Mode = _runtimeInfo.Mode,
                       Branch = _configFileProvider.Branch,
                       Authentication = _configFileProvider.AuthenticationMethod,
                       SqliteVersion = _database.Version.ToString(),
                       UrlBase = _configFileProvider.UrlBase,
                       RuntimeVersion = _platformInfo.Version.ToString(),
                       RuntimeName = PlatformInfo.Platform,
                       StartTime = _runtimeInfo.StartTime,
                       PackageVersion = _deploymentInfoProvider.PackageVersion,
                       PackageAuthor = _deploymentInfoProvider.PackageAuthor,
                       PackageUpdateMechanism = _deploymentInfoProvider.PackageUpdateMechanism,
                       PackageUpdateMechanismMessage = _deploymentInfoProvider.PackageUpdateMechanismMessage
            };

            return Ok(value);
        }

        [HttpGet("routes")]
        public IActionResult GetRoutes()
        {
            throw new NotImplementedException("IRouteCacheProvider is from Nancy");
            //return Ok(_routeCacheProvider.GetCache().Values);
        }

        [HttpPost("shutdown")]
        public IActionResult Shutdown()
        {
            Task.Factory.StartNew(() => _lifecycleService.Shutdown());
            return Ok(new { ShuttingDown = true });
        }

        [HttpPost("restart")]
        public IActionResult Restart()
        {
            Task.Factory.StartNew(() => _lifecycleService.Restart());
            return Ok(new { Restarting = true });
        }
    }
}
