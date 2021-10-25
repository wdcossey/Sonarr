using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace Sonarr.Http.Frontend.Mappers
{
    [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
    [Route("logfile")]
    public class LogFileMapperController : PhysicalFileControllerBase
    {
        private readonly IAppFolderInfo _appFolderInfo;

        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        public LogFileMapperController(
            ILogger<LogFileMapperController> logger,
            IAppFolderInfo appFolderInfo)
            : base(logger)
        {
            _appFolderInfo = appFolderInfo;
        }

        [HttpGet("{filename:required:regex([[-.a-zA-Z0-9]]+?\\.txt)}")]
        public IActionResult GetLogFile(string filename)
        {
            var filePath = Path.Combine(_appFolderInfo.GetLogFolder(), filename);
            return GetPhysicalFile(filePath);
        }
    }
}