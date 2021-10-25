using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Backup;

namespace Sonarr.Http.Frontend.Mappers
{
    [Route("backup")]
    public class BackupFileMapperController : PhysicalFileControllerBase
    {
        private readonly IBackupService _backupService;

        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        public BackupFileMapperController(
            ILogger<BackupFileMapperController> logger,
            IBackupService backupService)
            : base(logger)
        {
            _backupService = backupService;
        }

        [HttpGet("{type:required:regex((manual|scheduled|update))}/{fileName:required:regex((nzbdrone|sonarr)_backup_(v[[0-9.]]+_)?[[._0-9]]+\\.zip)}")]
        public IActionResult GetResponse(string type, string fileName)
        {
            var path = Path.Combine(_backupService.GetBackupFolder(), type, fileName);
            return GetPhysicalFile(path);
        }
    }
}
