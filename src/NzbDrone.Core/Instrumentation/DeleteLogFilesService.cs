using System.Threading.Tasks;
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

    public class DeleteLogFilesService : IDeleteLogFilesService, IExecuteAsync<DeleteLogFilesCommand>, IExecuteAsync<DeleteUpdateLogFilesCommand>
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

        public Task ExecuteAsync(DeleteLogFilesCommand message)
        {
            _logger.LogDebug("Deleting all files in: {LogFolder}", _appFolderInfo.GetLogFolder());
            _diskProvider.EmptyFolder(_appFolderInfo.GetLogFolder());
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(DeleteUpdateLogFilesCommand message)
        {
            _logger.LogDebug("Deleting all files in: {UpdateLogFolder}", _appFolderInfo.GetUpdateLogFolder());
            _diskProvider.EmptyFolder(_appFolderInfo.GetUpdateLogFolder());
            return Task.CompletedTask;
        }
    }
}
