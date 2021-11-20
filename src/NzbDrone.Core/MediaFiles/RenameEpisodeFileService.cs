using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MediaFiles
{
    public interface IRenameEpisodeFileService
    {
        List<RenameEpisodeFilePreview> GetRenamePreviews(int seriesId);
        List<RenameEpisodeFilePreview> GetRenamePreviews(int seriesId, int seasonNumber);
    }

    public class RenameEpisodeFileService : IRenameEpisodeFileService,
                                            IExecuteAsync<RenameFilesCommand>,
                                            IExecuteAsync<RenameSeriesCommand>
    {
        private readonly ISeriesService _seriesService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMoveEpisodeFiles _episodeFileMover;
        private readonly IEventAggregator _eventAggregator;
        private readonly IEpisodeService _episodeService;
        private readonly IBuildFileNames _filenameBuilder;
        private readonly IDiskProvider _diskProvider;
        private readonly ILogger<RenameEpisodeFileService> _logger;

        public RenameEpisodeFileService(ISeriesService seriesService,
                                        IMediaFileService mediaFileService,
                                        IMoveEpisodeFiles episodeFileMover,
                                        IEventAggregator eventAggregator,
                                        IEpisodeService episodeService,
                                        IBuildFileNames filenameBuilder,
                                        IDiskProvider diskProvider,
                                        ILogger<RenameEpisodeFileService> logger)
        {
            _seriesService = seriesService;
            _mediaFileService = mediaFileService;
            _episodeFileMover = episodeFileMover;
            _eventAggregator = eventAggregator;
            _episodeService = episodeService;
            _filenameBuilder = filenameBuilder;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<RenameEpisodeFilePreview> GetRenamePreviews(int seriesId)
        {
            var series = _seriesService.GetSeries(seriesId);
            var episodes = _episodeService.GetEpisodeBySeries(seriesId);
            var files = _mediaFileService.GetFilesBySeries(seriesId);

            return GetPreviews(series, episodes, files)
                .OrderByDescending(e => e.SeasonNumber)
                .ThenByDescending(e => e.EpisodeNumbers.First())
                .ToList();
        }

        public List<RenameEpisodeFilePreview> GetRenamePreviews(int seriesId, int seasonNumber)
        {
            var series = _seriesService.GetSeries(seriesId);
            var episodes = _episodeService.GetEpisodesBySeason(seriesId, seasonNumber);
            var files = _mediaFileService.GetFilesBySeason(seriesId, seasonNumber);

            return GetPreviews(series, episodes, files)
                .OrderByDescending(e => e.EpisodeNumbers.First()).ToList();
        }

        private IEnumerable<RenameEpisodeFilePreview> GetPreviews(Series series, List<Episode> episodes, List<EpisodeFile> files)
        {
            foreach (var f in files)
            {
                var file = f;
                var episodesInFile = episodes.Where(e => e.EpisodeFileId == file.Id).ToList();
                var episodeFilePath = Path.Combine(series.Path, file.RelativePath);

                if (!episodesInFile.Any())
                {
                    _logger.LogWarning("File ({EpisodeFilePath}) is not linked to any episodes", episodeFilePath);
                    continue;
                }

                var seasonNumber = episodesInFile.First().SeasonNumber;
                var newPath = _filenameBuilder.BuildFilePath(episodesInFile, series, file, Path.GetExtension(episodeFilePath));

                if (!episodeFilePath.PathEquals(newPath, StringComparison.Ordinal))
                {
                    yield return new RenameEpisodeFilePreview
                    {
                        SeriesId = series.Id,
                        SeasonNumber = seasonNumber,
                        EpisodeNumbers = episodesInFile.Select(e => e.EpisodeNumber).ToList(),
                        EpisodeFileId = file.Id,
                        ExistingPath = file.RelativePath,
                        NewPath = series.Path.GetRelativePath(newPath)
                    };
                }
            }
        }

        private List<RenamedEpisodeFile> RenameFiles(List<EpisodeFile> episodeFiles, Series series)
        {
            var renamed = new List<RenamedEpisodeFile>();

            foreach (var episodeFile in episodeFiles)
            {
                var previousRelativePath = episodeFile.RelativePath;
                var previousPath = Path.Combine(series.Path, episodeFile.RelativePath);

                try
                {
                    _logger.LogDebug("Renaming episode file: {EpisodeFile}", episodeFile);
                    _episodeFileMover.MoveEpisodeFile(episodeFile, series);

                    _mediaFileService.Update(episodeFile);

                    renamed.Add(new RenamedEpisodeFile
                                {
                                    EpisodeFile = episodeFile,
                                    PreviousRelativePath = previousRelativePath,
                                    PreviousPath = previousPath
                                });

                    _logger.LogDebug("Renamed episode file: {EpisodeFile}", episodeFile);

                    _eventAggregator.PublishEvent(new EpisodeFileRenamedEvent(series, episodeFile, previousPath));
                }
                catch (SameFilenameException ex)
                {
                    _logger.LogDebug("File not renamed, source and destination are the same: {Filename}", ex.Filename);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rename file {PreviousPath}", previousPath);
                }
            }

            if (renamed.Any())
            {
                _diskProvider.RemoveEmptySubfolders(series.Path);

                _eventAggregator.PublishEvent(new SeriesRenamedEvent(series, renamed));
            }

            return renamed;
        }

        public Task ExecuteAsync(RenameFilesCommand message)
        {
            var series = _seriesService.GetSeries(message.SeriesId);
            var episodeFiles = _mediaFileService.Get(message.Files);

            _logger.ProgressInfo("Renaming {0} files for {1}", episodeFiles.Count, series.Title);
            RenameFiles(episodeFiles, series);
            _logger.ProgressInfo("Selected episode files renamed for {0}", series.Title);

            _eventAggregator.PublishEvent(new RenameCompletedEvent());
            
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(RenameSeriesCommand message)
        {
            _logger.LogDebug("Renaming all files for selected series");
            var seriesToRename = _seriesService.GetSeries(message.SeriesIds);

            foreach (var series in seriesToRename)
            {
                var episodeFiles = _mediaFileService.GetFilesBySeries(series.Id);
                _logger.ProgressInfo("Renaming all files in series: {0}", series.Title);
                RenameFiles(episodeFiles, series);
                _logger.ProgressInfo("All episode files renamed for {0}", series.Title);
            }

            _eventAggregator.PublishEvent(new RenameCompletedEvent());
            
            return Task.CompletedTask;
        }
    }
}
