using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;

namespace Sonarr.Http.Frontend.Mappers
{
    [AllowAnonymous]
    public class BrowserConfigController : PhysicalFileControllerBase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IConfigFileProvider _configFileProvider;

        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        public BrowserConfigController(
            ILogger<BrowserConfigController> logger,
            IAppFolderInfo appFolderInfo,
            IConfigFileProvider configFileProvider)
            : base(logger)
        {
            _appFolderInfo = appFolderInfo;
            _configFileProvider = configFileProvider;
        }

        [HttpGet("{fileName:required:regex(browserconfig(\\.xml)?)}")]
        [HttpGet("content/images/icons/{fileName:required:regex(browserconfig(\\.xml)?)}")]
        public IActionResult GetBrowserConfigFile(string fileName)
        {
            var filePath = GetFilePath(fileName);
            return GetPhysicalFile(filePath);
        }

        private string GetFilePath(string fileName)
        {
            var path = Path.Combine(_appFolderInfo.StartUpFolder, _configFileProvider.UiFolder, "Content", "Images", "Icons", fileName);
            return Path.ChangeExtension(path, "xml");
        }
    }
}
