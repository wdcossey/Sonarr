using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.EpisodeImport;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MediaFiles
{
    public interface IDiskScanService
    {
        void Scan(Series series);
        string[] GetVideoFiles(string path, bool allDirectories = true);
        string[] GetNonVideoFiles(string path, bool allDirectories = true);
        List<string> FilterPaths(string basePath, IEnumerable<string> files);
    }

    public class DiskScanService : IDiskScanService, IExecuteAsync<RescanSeriesCommand>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IImportApprovedEpisodes _importApprovedEpisodes;
        private readonly IConfigService _configService;
        private readonly ISeriesService _seriesService;
        private readonly IMediaFileTableCleanupService _mediaFileTableCleanupService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<DiskScanService> _logger;

        public DiskScanService(IDiskProvider diskProvider,
                               IMakeImportDecision importDecisionMaker,
                               IImportApprovedEpisodes importApprovedEpisodes,
                               IConfigService configService,
                               ISeriesService seriesService,
                               IMediaFileTableCleanupService mediaFileTableCleanupService,
                               IRootFolderService rootFolderService,
                               IEventAggregator eventAggregator,
                               ILogger<DiskScanService> logger)
        {
            _diskProvider = diskProvider;
            _importDecisionMaker = importDecisionMaker;
            _importApprovedEpisodes = importApprovedEpisodes;
            _configService = configService;
            _seriesService = seriesService;
            _mediaFileTableCleanupService = mediaFileTableCleanupService;
            _rootFolderService = rootFolderService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        private static readonly Regex ExcludedSubFoldersRegex = new Regex(@"(?:\\|\/|^)(?:extras|@eadir|\.@__thumb|extrafanart|plex versions|\.[^\\/]+)(?:\\|\/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ExcludedFilesRegex = new Regex(@"^\._|^Thumbs\.db$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public void Scan(Series series)
        {
            var rootFolder = _rootFolderService.GetBestRootFolderPath(series.Path);

            var seriesFolderExists = _diskProvider.FolderExists(series.Path);

            if (!seriesFolderExists)
            {
                if (!_diskProvider.FolderExists(rootFolder))
                {
                    _logger.LogWarning("Series' root folder ({RootFolder}) doesn't exist.", rootFolder);
                    _eventAggregator.PublishEvent(new SeriesScanSkippedEvent(series, SeriesScanSkippedReason.RootFolderDoesNotExist));
                    return;
                }

                if (_diskProvider.FolderEmpty(rootFolder))
                {
                    _logger.LogWarning("Series' root folder ({RootFolder}) is empty.", rootFolder);
                    _eventAggregator.PublishEvent(new SeriesScanSkippedEvent(series, SeriesScanSkippedReason.RootFolderIsEmpty));
                    return;
                }
            }

            _logger.ProgressInfo("Scanning {0}", series.Title);

            if (!seriesFolderExists)
            {
                if (_configService.CreateEmptySeriesFolders)
                {
                    if (_configService.DeleteEmptyFolders)
                    {
                        _logger.LogDebug("Not creating missing series folder: {Path} because delete empty series folders is enabled", series.Path);
                    }
                    else
                    {
                        _logger.LogDebug("Creating missing series folder: {Path}", series.Path);

                        _diskProvider.CreateFolder(series.Path);
                        SetPermissions(series.Path);
                    }
                }
                else
                {
                    _logger.LogDebug("Series folder doesn't exist: {Path}", series.Path);
                }

                CleanMediaFiles(series, new List<string>());
                CompletedScanning(series);

                return;
            }

            var videoFilesStopwatch = Stopwatch.StartNew();
            var mediaFileList = FilterPaths(series.Path, GetVideoFiles(series.Path)).ToList();
            videoFilesStopwatch.Stop();
            _logger.LogTrace("Finished getting episode files for: {Series} [{Elapsed}]", series, videoFilesStopwatch.Elapsed);

            CleanMediaFiles(series, mediaFileList);

            var decisionsStopwatch = Stopwatch.StartNew();
            var decisions = _importDecisionMaker.GetImportDecisions(mediaFileList, series);
            decisionsStopwatch.Stop();
            _logger.LogTrace("Import decisions complete for: {Series} [{Elapsed}]", series, decisionsStopwatch.Elapsed);
            _importApprovedEpisodes.Import(decisions, false);

            RemoveEmptySeriesFolder(series.Path);
            CompletedScanning(series);
        }

        private void CleanMediaFiles(Series series, List<string> mediaFileList)
        {
            _logger.LogDebug("{Series} Cleaning up media files in DB", series);
            _mediaFileTableCleanupService.Clean(series, mediaFileList);
        }

        private void CompletedScanning(Series series)
        {
            _logger.LogInformation("Completed scanning disk for {Title}", series.Title);
            _eventAggregator.PublishEvent(new SeriesScannedEvent(series));
        }

        public string[] GetVideoFiles(string path, bool allDirectories = true)
        {
            _logger.LogDebug("Scanning '{Path}' for video files", path);

            var searchOption = allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var filesOnDisk = _diskProvider.GetFiles(path, searchOption).ToList();

            var mediaFileList = filesOnDisk.Where(file => MediaFileExtensions.Extensions.Contains(Path.GetExtension(file)))
                                           .ToList();

            _logger.LogTrace("{Count} files were found in {Path}", filesOnDisk.Count, path);
            _logger.LogDebug("{Count} video files were found in {Path}", mediaFileList.Count, path);

            return mediaFileList.ToArray();
        }

        public string[] GetNonVideoFiles(string path, bool allDirectories = true)
        {
            _logger.LogDebug("Scanning '{Path}' for non-video files", path);

            var searchOption = allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var filesOnDisk = _diskProvider.GetFiles(path, searchOption).ToList();

            var mediaFileList = filesOnDisk.Where(file => !MediaFileExtensions.Extensions.Contains(Path.GetExtension(file)))
                                           .ToList();

            _logger.LogTrace("{Count} files were found in {Path}", filesOnDisk.Count, path);
            _logger.LogDebug("{Count} non-video files were found in {Path}", mediaFileList.Count, path);

            return mediaFileList.ToArray();
        }

        public List<string> FilterPaths(string basePath, IEnumerable<string> paths)
        {
            return paths.Where(path => !ExcludedSubFoldersRegex.IsMatch(basePath.GetRelativePath(path)))
                        .Where(path => !ExcludedFilesRegex.IsMatch(Path.GetFileName(path)))
                        .ToList();
        }

        private void SetPermissions(string path)
        {
            if (!_configService.SetPermissionsLinux)
            {
                return;
            }

            try
            {
                _diskProvider.SetPermissions(path, _configService.ChmodFolder, _configService.ChownGroup);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to apply permissions to: {Path}", path);
                _logger.LogDebug(ex, "{Message}", ex.Message);
            }
        }

        private void RemoveEmptySeriesFolder(string path)
        {
            if (_configService.DeleteEmptyFolders)
            {
                _diskProvider.RemoveEmptySubfolders(path);

                if (_diskProvider.FolderEmpty(path))
                {
                    _diskProvider.DeleteFolder(path, true);
                }
            }
        }

        public Task ExecuteAsync(RescanSeriesCommand message)
        {
            if (message.SeriesId.HasValue)
            {
                var series = _seriesService.GetSeries(message.SeriesId.Value);
                Scan(series);
            }

            else
            {
                var allSeries = _seriesService.GetAllSeries();

                foreach (var series in allSeries)
                {
                    Scan(series);
                }
            }
            
            return Task.CompletedTask;
        }
    }
}
