using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;

namespace Sonarr.Http.Frontend.Mappers
{
    [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
    public class ManifestMapperController : PhysicalFileControllerBase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IConfigFileProvider _configFileProvider;

        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        public ManifestMapperController(
            ILogger<ManifestMapperController> logger,
            IAppFolderInfo appFolderInfo,
            IConfigFileProvider configFileProvider)
            : base(logger)
        {
            _appFolderInfo = appFolderInfo;
            _configFileProvider = configFileProvider;
        }

        [HttpGet("{fileName:required:regex(manifest(\\.json)?)}")]
        [HttpGet("content/images/icons/{fileName:required:regex(manifest(\\.json)?)}")]
        public IActionResult GetManifestFile(string fileName)
        {
            var filePath = GetFilePath(fileName);
            return GetPhysicalFile(filePath);
        }

        private string GetFilePath(string fileName)
        {
            var path = Path.Combine(_appFolderInfo.StartUpFolder, _configFileProvider.UiFolder, "Content", "Images", "Icons", fileName);
            return Path.ChangeExtension(path, "json");
        }
    }
}