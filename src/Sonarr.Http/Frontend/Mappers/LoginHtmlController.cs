using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;

namespace Sonarr.Http.Frontend.Mappers
{
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
    [Route("login")]
    public class LoginHtmlController : PhysicalFileControllerBase
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly string _htmlPath;

        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        public LoginHtmlController(
            ILogger<LoginHtmlController> logger,
            IAppFolderInfo appFolderInfo,
            IConfigFileProvider configFileProvider)
            : base(logger)
        {
            _configFileProvider = configFileProvider;
            _htmlPath = Path.Combine(appFolderInfo.StartUpFolder, configFileProvider.UiFolder, "login.html");
        }

        [HttpGet]
        public IActionResult Get()
        {
            return _configFileProvider.AuthenticationMethod == AuthenticationType.None
                ? Redirect(_configFileProvider.UrlBase + "/")
                : GetPhysicalFile(_htmlPath);
        }
    }
}
