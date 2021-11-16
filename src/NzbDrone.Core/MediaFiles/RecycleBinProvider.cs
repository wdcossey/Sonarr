using System;
using System.IO;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles
{
    public interface IRecycleBinProvider
    {
        void DeleteFolder(string path);
        void DeleteFile(string path, string subfolder = "");
        void Empty();
        void Cleanup();
    }

    public class RecycleBinProvider : IExecute<CleanUpRecycleBinCommand>, IRecycleBinProvider
    {
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly ILogger<RecycleBinProvider> _logger;


        public RecycleBinProvider(IDiskTransferService diskTransferService,
                                  IDiskProvider diskProvider,
                                  IConfigService configService,
                                  ILogger<RecycleBinProvider> logger)
        {
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _configService = configService;
            _logger = logger;
        }

        public void DeleteFolder(string path)
        {
            _logger.LogInformation("Attempting to send '{Path}' to recycling bin", path);
            var recyclingBin = _configService.RecycleBin;

            if (string.IsNullOrWhiteSpace(recyclingBin))
            {
                _logger.LogInformation("Recycling Bin has not been configured, deleting permanently. {Path}", path);
                _diskProvider.DeleteFolder(path, true);
                _logger.LogDebug("Folder has been permanently deleted: {Path}", path);
            }

            else
            {
                var destination = Path.Combine(recyclingBin, new DirectoryInfo(path).Name);

                _logger.LogDebug("Moving '{Path}' to '{Destination}'", path, destination);
                _diskTransferService.TransferFolder(path, destination, TransferMode.Move);

                _logger.LogDebug("Setting last accessed: {Path}", path);
                _diskProvider.FolderSetLastWriteTime(destination, DateTime.UtcNow);
                foreach (var file in _diskProvider.GetFiles(destination, SearchOption.AllDirectories))
                {
                    SetLastWriteTime(file, DateTime.UtcNow);
                }

                _logger.LogDebug("Folder has been moved to the recycling bin: {Destination}", destination);
            }
        }

        public void DeleteFile(string path, string subfolder = "")
        {
            _logger.LogDebug("Attempting to send '{Path}' to recycling bin", path);
            var recyclingBin = _configService.RecycleBin;

            if (string.IsNullOrWhiteSpace(recyclingBin))
            {
                _logger.LogInformation("Recycling Bin has not been configured, deleting permanently. {Path}", path);

                if (OsInfo.IsWindows)
                {
                    _logger.LogDebug("{FileAttributes}", _diskProvider.GetFileAttributes(path));
                }

                _diskProvider.DeleteFile(path);
                _logger.LogDebug("File has been permanently deleted: {Path}", path);
            }

            else
            {
                var fileInfo = new FileInfo(path);
                var destinationFolder = Path.Combine(recyclingBin, subfolder);
                var destination = Path.Combine(destinationFolder, fileInfo.Name);

                try
                {
                    _logger.LogDebug("Creating folder [{DestinationFolder}]", destinationFolder);
                    _diskProvider.CreateFolder(destinationFolder);
                }
                catch (IOException e)
                {
                    _logger.LogError(e, "Unable to create the folder '{DestinationFolder}' in the recycling bin for the file '{FileInfoName}'", destinationFolder, fileInfo.Name);
                    throw;
                }

                var index = 1;
                while (_diskProvider.FileExists(destination))
                {
                    index++;
                    if (fileInfo.Extension.IsNullOrWhiteSpace())
                    {
                        destination = Path.Combine(destinationFolder, fileInfo.Name + "_" + index);
                    }
                    else
                    {
                        destination = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(fileInfo.Name) + "_" + index + fileInfo.Extension);
                    }
                }

                try
                {
                    _logger.LogDebug("Moving '{Path}' to '{Destination}'", path, destination);
                    _diskTransferService.TransferFile(path, destination, TransferMode.Move);
                }
                catch (IOException e)
                {
                    _logger.LogError(e, "Unable to move '{Path}' to the recycling bin: '{Destination}'", path, destination);
                    throw;
                }

                SetLastWriteTime(destination, DateTime.UtcNow);

                _logger.LogDebug("File has been moved to the recycling bin: {Destination}", destination);
            }
        }

        public void Empty()
        {
            if (string.IsNullOrWhiteSpace(_configService.RecycleBin))
            {
                _logger.LogInformation("Recycle Bin has not been configured, cannot empty.");
                return;
            }

            _logger.LogInformation("Removing all items from the recycling bin");

            foreach (var folder in _diskProvider.GetDirectories(_configService.RecycleBin))
            {
                _diskProvider.DeleteFolder(folder, true);
            }

            foreach (var file in _diskProvider.GetFiles(_configService.RecycleBin, SearchOption.TopDirectoryOnly))
            {
                _diskProvider.DeleteFile(file);
            }

            _logger.LogDebug("Recycling Bin has been emptied.");
        }

        public void Cleanup()
        {
            if (string.IsNullOrWhiteSpace(_configService.RecycleBin))
            {
                _logger.LogInformation("Recycle Bin has not been configured, cannot cleanup.");
                return;
            }

            var cleanupDays = _configService.RecycleBinCleanupDays;

            if (cleanupDays == 0)
            {
                _logger.LogInformation("Automatic cleanup of Recycle Bin is disabled");
                return;
            }

            _logger.LogInformation("Removing items older than {CleanupDays} days from the recycling bin", cleanupDays);

            foreach (var file in _diskProvider.GetFiles(_configService.RecycleBin, SearchOption.AllDirectories))
            {
                if (_diskProvider.FileGetLastWrite(file).AddDays(cleanupDays) > DateTime.UtcNow)
                {
                    _logger.LogDebug("File hasn't expired yet, skipping: {File}", file);
                    continue;
                }

                _diskProvider.DeleteFile(file);
            }

            _diskProvider.RemoveEmptySubfolders(_configService.RecycleBin);

            _logger.LogDebug("Recycling Bin has been cleaned up.");
        }

        private void SetLastWriteTime(string file, DateTime dateTime)
        {
            // Swallow any IOException that may be thrown due to "Invalid parameter"
            try
            {
                _diskProvider.FileSetLastWriteTime(file, dateTime);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public void Execute(CleanUpRecycleBinCommand message)
        {
            Cleanup();
        }
    }
}
