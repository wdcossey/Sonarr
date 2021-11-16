using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Instrumentation.Commands;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Instrumentation
{
    public interface IDeleteLogFilesService
    {
    }

    public class DeleteLogFilesService : IDeleteLogFilesService, IExecute<DeleteLogFilesCommand>, IExecute<DeleteUpdateLogFilesCommand>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly ILogger<DeleteLogFilesService> _logger;

        public DeleteLogFilesService(IDiskProvider diskProvider, IAppFolderInfo appFolderInfo, ILogger<DeleteLogFilesService> logger)
        {
            _diskProvider = diskProvider;
            _appFolderInfo = appFolderInfo;
            _logger = logger;
        }

        public void Execute(DeleteLogFilesCommand message)
        {
            _logger.LogDebug("Deleting all files in: {LogFolder}", _appFolderInfo.GetLogFolder());
            _diskProvider.EmptyFolder(_appFolderInfo.GetLogFolder());
        }

        public void Execute(DeleteUpdateLogFilesCommand message)
        {
            _logger.LogDebug("Deleting all files in: {UpdateLogFolder}", _appFolderInfo.GetUpdateLogFolder());
            _diskProvider.EmptyFolder(_appFolderInfo.GetUpdateLogFolder());
        }
    }
}
