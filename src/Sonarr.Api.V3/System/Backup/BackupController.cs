using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Crypto;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Backup;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.System.Backup
{
    [ApiController]
    [SonarrApiRoute("system/backup", RouteVersion.V3)]
    public class BackupController : ControllerBase
    {
        private readonly IBackupService _backupService;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;

        private static readonly List<string> ValidExtensions = new() { ".zip", ".db", ".xml" };

        public BackupController(
            IBackupService backupService,
            IAppFolderInfo appFolderInfo,
            IDiskProvider diskProvider)
        {
            _backupService = backupService;
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
        }

        [HttpGet]
        public IActionResult GetBackupFiles()
        {
            var backups = _backupService.GetBackups();

            var result = backups.Select(b => new BackupResource
                {
                    Id = GetBackupId(b),
                    Name = b.Name,
                    Path = $"/backup/{b.Type.ToString().ToLower()}/{b.Name}",
                    Type = b.Type,
                    Time = b.Time
                })
                .OrderByDescending(b => b.Time);

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public IActionResult DeleteBackup(int id)
        {
            var backup = GetBackup(id);
            var path = GetBackupPath(backup);

            if (!_diskProvider.FileExists(path))
                return NotFound();

            _diskProvider.DeleteFile(path);

            return Ok(new object());
        }


        [HttpPost("restore/{id:int:required}")]
        public IActionResult Restore(int id)
        {
            var backup = GetBackup(id);

            if (backup == null)
                return NotFound();

            var path = GetBackupPath(backup);

            _backupService.Restore(path);

            return Ok(new { RestartRequired = true });
        }

        [HttpPost("restore/upload")]
        public IActionResult UploadAndRestore()
        {
            var files = Request.Form.Files;

            if (files.Empty())
                return BadRequest("file must be provided");

            var file = files.Single();
            var extension = Path.GetExtension(file.Name);

            if (!ValidExtensions.Contains(extension))
                return StatusCode(StatusCodes.Status415UnsupportedMediaType, $"Invalid extension, must be one of: {ValidExtensions.Join(", ")}");

            var path = Path.Combine(_appFolderInfo.TempFolder, $"sonarr_backup_restore{extension}");

            using var fileStream = file.OpenReadStream();
            _diskProvider.SaveStream(fileStream, path);
            _backupService.Restore(path);

            // Cleanup restored file
            _diskProvider.DeleteFile(path);

            return Ok(new { RestartRequired = true });
        }

        private string GetBackupPath(NzbDrone.Core.Backup.Backup backup)
            => Path.Combine(_backupService.GetBackupFolder(backup.Type), backup.Name);

        private int GetBackupId(NzbDrone.Core.Backup.Backup backup)
            => HashConverter.GetHashInt31($"backup-{backup.Type}-{backup.Name}");

        private NzbDrone.Core.Backup.Backup GetBackup(int id)
            => _backupService.GetBackups().SingleOrDefault(b => GetBackupId(b) == id);
    }
}
