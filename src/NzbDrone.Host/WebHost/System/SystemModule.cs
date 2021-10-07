using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Lifecycle;

namespace NzbDrone.Host.WebHost.System
{
    //public class SystemModule : EmbedIO.WebApi.WebApiModule
    public class SystemModule : EmbedIO.WebApi.WebApiController
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IRuntimeInfo _runtimeInfo;
        private readonly IPlatformInfo _platformInfo;
        private readonly IOsInfo _osInfo;
        //private readonly IRouteCacheProvider _routeCacheProvider;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IMainDatabase _database;
        private readonly ILifecycleService _lifecycleService;

        public SystemModule(
            IAppFolderInfo appFolderInfo,
            IRuntimeInfo runtimeInfo,
            IPlatformInfo platformInfo,
            IOsInfo osInfo,
            //IRouteCacheProvider routeCacheProvider,
            IConfigFileProvider configFileProvider,
            IMainDatabase database,
            ILifecycleService lifecycleService)
            //: base("/api/v3/system")
        {
            _appFolderInfo = appFolderInfo;
            _runtimeInfo = runtimeInfo;
            _platformInfo = platformInfo;
            _osInfo = osInfo;
            //_routeCacheProvider = routeCacheProvider;
            _configFileProvider = configFileProvider;
            _database = database;
            _lifecycleService = lifecycleService;
        }

        /*protected override Task OnPathNotFoundAsync(IHttpContext context)
        {
            return base.OnPathNotFoundAsync(context);
        }*/

        [Route(HttpVerbs.Get, "/status")]
        public Task<object> GetStatusAsync()
        {
            return Task.FromResult<object>(new
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
                Branch = _configFileProvider.Branch,
                Authentication = _configFileProvider.AuthenticationMethod,
                SqliteVersion = _database.Version.ToString(),
                UrlBase = _configFileProvider.UrlBase,
                RuntimeVersion = _platformInfo.Version.ToString(),
                RuntimeName = PlatformInfo.Platform
            });
        }


    }
}
