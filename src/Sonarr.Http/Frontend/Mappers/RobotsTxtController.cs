using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;

namespace Sonarr.Http.Frontend.Mappers
{
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
    public class RobotsTxtController : PhysicalFileControllerBase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IConfigFileProvider _configFileProvider;

        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        public RobotsTxtController(
            ILogger<RobotsTxtController> logger,
            IAppFolderInfo appFolderInfo,
            IConfigFileProvider configFileProvider)
            : base(logger)
        {
            _appFolderInfo = appFolderInfo;
            _configFileProvider = configFileProvider;
        }

        [HttpGet("{fileName:required:regex(robots\\.txt)}")]
        public IActionResult GetRobotsFile(string fileName)
        {
            var filePath = Path.Combine(_appFolderInfo.StartUpFolder, _configFileProvider.UiFolder, "Content", fileName);
            return GetPhysicalFile(filePath);
        }
    }
}
