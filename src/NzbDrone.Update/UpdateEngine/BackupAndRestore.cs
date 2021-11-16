using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Update.UpdateEngine
{
    public interface IBackupAndRestore
    {
        void Backup(string source);
        void Restore(string target);
    }

    public class BackupAndRestore : IBackupAndRestore
    {
        private readonly IDiskTransferService _diskTransferService;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly ILogger<BackupAndRestore> _logger;

        public BackupAndRestore(IDiskTransferService diskTransferService,
                                IAppFolderInfo appFolderInfo,
                                ILogger<BackupAndRestore> logger)
        {
            _diskTransferService = diskTransferService;
            _appFolderInfo = appFolderInfo;
            _logger = logger;
        }

        public void Backup(string source)
        {
            _logger.LogInformation("Creating backup of existing installation");
            _diskTransferService.MirrorFolder(source, _appFolderInfo.GetUpdateBackUpFolder());
        }

        public void Restore(string target)
        {
            _logger.LogInformation("Attempting to rollback upgrade");
            var count = _diskTransferService.MirrorFolder(_appFolderInfo.GetUpdateBackUpFolder(), target);
            _logger.LogInformation("Rolled back {Count} files", count);
        }
    }
}
