using System;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Update.UpdateEngine
{
    public interface IBackupAppData
    {
        void Backup();
    }

    public class BackupAppData : IBackupAppData
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly ILogger<BackupAppData> _logger;

        public BackupAppData(IAppFolderInfo appFolderInfo,
                             IDiskProvider diskProvider,
                             IDiskTransferService diskTransferService,
                             ILogger<BackupAppData> logger)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _diskTransferService = diskTransferService;
            _logger = logger;
        }

        public void Backup()
        {
            _logger.LogInformation("Backing up appdata (database/config)");
            var backupFolderAppData = _appFolderInfo.GetUpdateBackUpAppDataFolder();

            if (_diskProvider.FolderExists(backupFolderAppData))
            {
                _diskProvider.EmptyFolder(backupFolderAppData);
            }
            else
            {
                _diskProvider.CreateFolder(backupFolderAppData);
            }

            try
            {
                _diskTransferService.TransferFile(_appFolderInfo.GetConfigPath(), _appFolderInfo.GetUpdateBackupConfigFile(), TransferMode.Copy);
                _diskTransferService.TransferFile(_appFolderInfo.GetDatabase(), _appFolderInfo.GetUpdateBackupDatabase(), TransferMode.Copy);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Couldn't create a data backup");
            }
        }
    }
}
