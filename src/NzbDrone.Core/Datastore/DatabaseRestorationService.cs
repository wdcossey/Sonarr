using System;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Datastore
{
    public interface IRestoreDatabase
    {
        void Restore();
    }

    public class DatabaseRestorationService : IRestoreDatabase
    {
        private readonly ILogger<DatabaseRestorationService> _logger;
        private readonly IDiskProvider _diskProvider;
        private readonly IAppFolderInfo _appFolderInfo;

        public DatabaseRestorationService(ILogger<DatabaseRestorationService> logger, IDiskProvider diskProvider, IAppFolderInfo appFolderInfo)
        {
            _logger = logger;
            _diskProvider = diskProvider;
            _appFolderInfo = appFolderInfo;
        }

        public void Restore()
        {
            var dbRestorePath = _appFolderInfo.GetDatabaseRestore();

            if (!_diskProvider.FileExists(dbRestorePath))
            {
                return;
            }

            try
            {
                _logger.LogInformation("Restoring Database");

                var dbPath = _appFolderInfo.GetDatabase();

                _diskProvider.DeleteFile(dbPath + "-shm");
                _diskProvider.DeleteFile(dbPath + "-wal");
                _diskProvider.DeleteFile(dbPath + "-journal");
                _diskProvider.DeleteFile(dbPath);

                _diskProvider.MoveFile(dbRestorePath, dbPath);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to restore database");
                throw;
            }
        }
    }
}
