using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Lifecycle;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.System
{
    [Authorize]
    [ApiController]
    [SonarrApiRoute("system", RouteVersion.V3)]
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
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
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
                       IsMonoRuntime = false,
                       IsMono = false,
                       IsLinux = OsInfo.IsLinux,
                       IsOsx = OsInfo.IsOsx,
                       IsWindows = OsInfo.IsWindows,
                       Mode = _runtimeInfo.Mode,
                       Branch = _configFileProvider.Branch,
                       Authentication = _configFileProvider.AuthenticationMethod,
                       SqliteVersion = _database.Version,
                       UrlBase = _configFileProvider.UrlBase,
                       RuntimeVersion = _platformInfo.Version,
                       RuntimeName = "dotNet",
                       StartTime = _runtimeInfo.StartTime,
                       PackageVersion = _deploymentInfoProvider.PackageVersion,
                       PackageAuthor = _deploymentInfoProvider.PackageAuthor,
                       PackageUpdateMechanism = _deploymentInfoProvider.PackageUpdateMechanism,
                       PackageUpdateMechanismMessage = _deploymentInfoProvider.PackageUpdateMechanismMessage
            };

            return Ok(value);
        }

        [HttpGet("routes")]
        public IActionResult GetRoutes() //TODO: What to do?!?
        {
            throw new NotImplementedException("IRouteCacheProvider is from Nancy");
            //return Ok(_routeCacheProvider.GetCache().Values);
        }

        [Authorize(Roles = "sonarr-shutdown")]
        [HttpPost("shutdown")]
        public IActionResult Shutdown()
        {
            Task.Factory.StartNew(() => _lifecycleService.Shutdown());
            return Ok(new { ShuttingDown = true });
        }

        [Authorize(Roles = "sonarr-restart")]
        [HttpPost("restart")]
        public IActionResult Restart()
        {
            Task.Factory.StartNew(() => _lifecycleService.Restart());
            return Ok(new { Restarting = true });
        }
    }
}
