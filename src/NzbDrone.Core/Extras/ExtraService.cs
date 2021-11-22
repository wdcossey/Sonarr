using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Extras
{
    public interface IExtraService
    {
        void ImportEpisode(LocalEpisode localEpisode, EpisodeFile episodeFile, bool isReadOnly);
    }

    public class ExtraService : IExtraService,
                                IHandleAsync<MediaCoversUpdatedEvent>,
                                IHandleAsync<EpisodeFolderCreatedEvent>,
                                IHandleAsync<SeriesScannedEvent>,
                                IHandleAsync<SeriesRenamedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IEpisodeService _episodeService;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly List<IManageExtraFiles> _extraFileManagers;
        private readonly ILogger<ExtraService> _logger;

        public ExtraService(IMediaFileService mediaFileService,
                            IEpisodeService episodeService,
                            IDiskProvider diskProvider,
                            IConfigService configService,
                            IEnumerable<IManageExtraFiles> extraFileManagers,
                            ILogger<ExtraService> logger)
        {
            _mediaFileService = mediaFileService;
            _episodeService = episodeService;
            _diskProvider = diskProvider;
            _configService = configService;
            _extraFileManagers = extraFileManagers.OrderBy(e => e.Order).ToList();
            _logger = logger;
        }

        public void ImportEpisode(LocalEpisode localEpisode, EpisodeFile episodeFile, bool isReadOnly)
        {
            ImportExtraFiles(localEpisode, episodeFile, isReadOnly);

            CreateAfterEpisodeImport(localEpisode.Series, episodeFile);
        }

        private void ImportExtraFiles(LocalEpisode localEpisode, EpisodeFile episodeFile, bool isReadOnly)
        {
            if (!_configService.ImportExtraFiles)
            {
                return;
            }

            var sourcePath = localEpisode.Path;
            var sourceFolder = _diskProvider.GetParentFolder(sourcePath);
            var sourceFileName = Path.GetFileNameWithoutExtension(sourcePath);
            var files = _diskProvider.GetFiles(sourceFolder, SearchOption.TopDirectoryOnly);

            var wantedExtensions = _configService.ExtraFileExtensions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                                     .Select(e => e.Trim(' ', '.'))
                                                                     .ToList();

            var matchingFilenames = files.Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(sourceFileName, StringComparison.InvariantCultureIgnoreCase)).ToList();
            var filteredFilenames = new List<string>();
            var hasNfo = false;

            foreach (var matchingFilename in matchingFilenames)
            {
                // Filter out duplicate NFO files

                if (matchingFilename.EndsWith(".nfo", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (hasNfo)
                    {
                        continue;
                    }

                    hasNfo = true;
                }

                filteredFilenames.Add(matchingFilename);
            }

            foreach (var matchingFilename in filteredFilenames)
            {
                var matchingExtension = wantedExtensions.FirstOrDefault(e => matchingFilename.EndsWith(e));

                if (matchingExtension == null)
                {
                    continue;
                }

                try
                {
                    foreach (var extraFileManager in _extraFileManagers)
                    {
                        var extension = Path.GetExtension(matchingFilename);
                        var extraFile = extraFileManager.Import(localEpisode.Series, episodeFile, matchingFilename, extension, isReadOnly);

                        if (extraFile != null)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to import extra file: {MatchingFilename}", matchingFilename);
                }
            }
        }

        private void CreateAfterEpisodeImport(Series series, EpisodeFile episodeFile)
        {
            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterEpisodeImport(series, episodeFile);
            }
        }

        public Task HandleAsync(MediaCoversUpdatedEvent message)
        {
            if (message.Updated)
            {
                var series = message.Series;

                foreach (var extraFileManager in _extraFileManagers)
                {
                    extraFileManager.CreateAfterMediaCoverUpdate(series);
                }
            }
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(SeriesScannedEvent message)
        {
            var series = message.Series;
            var episodeFiles = GetEpisodeFiles(series.Id);

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterSeriesScan(series, episodeFiles);
            }
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(EpisodeFolderCreatedEvent message)
        {
            var series = message.Series;

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterEpisodeFolder(series, message.SeriesFolder, message.SeasonFolder);
            }
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(SeriesRenamedEvent message)
        {
            var series = message.Series;
            var episodeFiles = GetEpisodeFiles(series.Id);

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.MoveFilesAfterRename(series, episodeFiles);
            }
            
            return Task.CompletedTask;
        }

        private List<EpisodeFile> GetEpisodeFiles(int seriesId)
        {
            var episodeFiles = _mediaFileService.GetFilesBySeries(seriesId);
            var episodes = _episodeService.GetEpisodeBySeries(seriesId);

            foreach (var episodeFile in episodeFiles)
            {
                var localEpisodeFile = episodeFile;
                episodeFile.Episodes = new LazyList<Episode>(episodes.Where(e => e.EpisodeFileId == localEpisodeFile.Id));
            }

            return episodeFiles;
        }
    }
}
