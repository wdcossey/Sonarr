using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Tv.Commands;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.Tv
{
    public class MoveSeriesService : IExecuteAsync<MoveSeriesCommand>, IExecuteAsync<BulkMoveSeriesCommand>
    {
        private readonly ISeriesService _seriesService;
        private readonly IBuildFileNames _filenameBuilder;
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<MoveSeriesService> _logger;

        public MoveSeriesService(ISeriesService seriesService,
                                 IBuildFileNames filenameBuilder,
                                 IDiskProvider diskProvider,
                                 IDiskTransferService diskTransferService,
                                 IEventAggregator eventAggregator,
                                 ILogger<MoveSeriesService> logger)
        {
            _seriesService = seriesService;
            _filenameBuilder = filenameBuilder;
            _diskProvider = diskProvider;
            _diskTransferService = diskTransferService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        private void MoveSingleSeries(Series series, string sourcePath, string destinationPath, int? index = null, int? total = null)
        {
            if (!_diskProvider.FolderExists(sourcePath))
            {
                _logger.LogDebug("Folder '{SourcePath}' for '{Title}' does not exist, not moving.", sourcePath, series.Title);
                return;
            }

            if (index != null && total != null)
            {
                _logger.ProgressInfo("Moving {Title} from '{SourcePath}' to '{DestinationPath}' ({Index}/{Total})", series.Title, sourcePath, destinationPath, index + 1, total);
            }
            else
            {
                _logger.ProgressInfo("Moving {Title} from '{SourcePath}' to '{DestinationPath}'", series.Title, sourcePath, destinationPath);
            }

            try
            {
                // Ensure the parent of the series folder exists, this will often just be the root folder, but
                // in cases where people are using subfolders for first letter (etc) it may not yet exist.
                _diskProvider.CreateFolder(new DirectoryInfo(destinationPath).Parent.FullName);
                _diskTransferService.TransferFolder(sourcePath, destinationPath, TransferMode.Move);

                _logger.ProgressInfo("{0} moved successfully to {1}", series.Title, series.Path);

                _eventAggregator.PublishEvent(new SeriesMovedEvent(series, sourcePath, destinationPath));
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Unable to move series from '{SourcePath}' to '{DestinationPath}'. Try moving files manually", sourcePath, destinationPath);

                RevertPath(series.Id, sourcePath);
            }
        }

        private void RevertPath(int seriesId, string path)
        {
            var series = _seriesService.GetSeries(seriesId);

            series.Path = path;
            _seriesService.UpdateSeries(series);
        }

        public Task ExecuteAsync(MoveSeriesCommand message)
        {
            var series = _seriesService.GetSeries(message.SeriesId);
            MoveSingleSeries(series, message.SourcePath, message.DestinationPath);
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(BulkMoveSeriesCommand message)
        {
            var seriesToMove = message.Series;
            var destinationRootFolder = message.DestinationRootFolder;

            _logger.ProgressInfo("Moving {0} series to '{1}'", seriesToMove.Count, destinationRootFolder);

            for (var index = 0; index < seriesToMove.Count; index++)
            {
                var s = seriesToMove[index];
                var series = _seriesService.GetSeries(s.SeriesId);
                var destinationPath = Path.Combine(destinationRootFolder, _filenameBuilder.GetSeriesFolder(series));

                MoveSingleSeries(series, s.SourcePath, destinationPath, index, seriesToMove.Count);
            }

            _logger.ProgressInfo("Finished moving {0} series to '{1}'", seriesToMove.Count, destinationRootFolder);
            
            return Task.CompletedTask;
        }
    }
}
