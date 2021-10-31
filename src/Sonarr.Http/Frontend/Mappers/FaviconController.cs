using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;

namespace Sonarr.Http.Frontend.Mappers
{
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class FaviconController : PhysicalFileControllerBase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IConfigFileProvider _configFileProvider;

        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        public FaviconController(
            ILogger<FaviconController> logger,
            IAppFolderInfo appFolderInfo,
            IConfigFileProvider configFileProvider)
            : base(logger)
        {
            _appFolderInfo = appFolderInfo;
            _configFileProvider = configFileProvider;
        }

        [HttpGet("favicon.ico")]
        public IActionResult GetFaviconFile()
        {
            var filePath = GetFilePath();
            return GetPhysicalFile(filePath);
        }

        private string GetFilePath()
        {
            var fileName = "favicon.ico";

            if (BuildInfo.IsDebug)
                fileName = "favicon-debug.ico";

            return Path.Combine(_appFolderInfo.StartUpFolder, _configFileProvider.UiFolder, "Content", "Images", "Icons", fileName);
        }
    }
}