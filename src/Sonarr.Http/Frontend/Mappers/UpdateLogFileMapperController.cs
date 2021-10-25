using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using Sonarr.Http.Extensions;

namespace Sonarr.Http.Frontend.Mappers
{
    [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
    [Route("updatelogfile")]
    public class UpdateLogFileMapperController  : PhysicalFileControllerBase
    {
        private readonly IAppFolderInfo _appFolderInfo;

        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        public UpdateLogFileMapperController(
            ILogger<UpdateLogFileMapperController> logger,
            IAppFolderInfo appFolderInfo)
            : base(logger)
        {
            _appFolderInfo = appFolderInfo;
        }

        [HttpGet("{filename:required:regex([[-.a-zA-Z0-9]]+?\\.txt)}")]
        public IActionResult GetUpdateLogFile(string filename)
        {
            var filePath = Path.Combine(_appFolderInfo.GetUpdateLogFolder(), filename);
            return GetPhysicalFile(filePath);
        }
    }
}