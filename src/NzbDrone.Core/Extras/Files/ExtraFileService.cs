using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.Extras.Files
{
    public interface IExtraFileService<TExtraFile>
        where TExtraFile : ExtraFile, new()
    {
        List<TExtraFile> GetFilesBySeries(int seriesId);
        List<TExtraFile> GetFilesByEpisodeFile(int episodeFileId);
        TExtraFile FindByPath(string path);
        void Upsert(TExtraFile extraFile);
        void Upsert(List<TExtraFile> extraFiles);
        void Delete(int id);
        void DeleteMany(IEnumerable<int> ids);
    }

    public abstract class ExtraFileService<TExtraFile> : IExtraFileService<TExtraFile>,
                                                         IHandleAsync<SeriesDeletedEvent>,
                                                         IHandleAsync<EpisodeFileDeletedEvent>
        where TExtraFile : ExtraFile, new()
    {
        private readonly IExtraFileRepository<TExtraFile> _repository;
        private readonly ISeriesService _seriesService;
        private readonly IDiskProvider _diskProvider;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly ILogger _logger;

        public ExtraFileService(IExtraFileRepository<TExtraFile> repository,
                                ISeriesService seriesService,
                                IDiskProvider diskProvider,
                                IRecycleBinProvider recycleBinProvider,
                                ILogger logger)
        {
            _repository = repository;
            _seriesService = seriesService;
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _logger = logger;
        }

        public List<TExtraFile> GetFilesBySeries(int seriesId)
        {
            return _repository.GetFilesBySeries(seriesId);
        }

        public List<TExtraFile> GetFilesByEpisodeFile(int episodeFileId)
        {
            return _repository.GetFilesByEpisodeFile(episodeFileId);
        }

        public TExtraFile FindByPath(string path)
        {
            return _repository.FindByPath(path);
        }

        public void Upsert(TExtraFile extraFile)
        {
            Upsert(new List<TExtraFile> { extraFile });
        }

        public void Upsert(List<TExtraFile> extraFiles)
        {
            extraFiles.ForEach(m =>
            {
                m.LastUpdated = DateTime.UtcNow;

                if (m.Id == 0)
                {
                    m.Added = m.LastUpdated;
                }
            });

            _repository.InsertMany(extraFiles.Where(m => m.Id == 0).ToList());
            _repository.UpdateMany(extraFiles.Where(m => m.Id > 0).ToList());
        }

        public void Delete(int id)
        {
            _repository.Delete(id);
        }

        public void DeleteMany(IEnumerable<int> ids)
        {
            _repository.DeleteMany(ids);
        }

        public Task HandleAsync(SeriesDeletedEvent message)
        {
            _logger.LogDebug("Deleting Extra from database for series: {Series}", message.Series);
            _repository.DeleteForSeries(message.Series.Id);
            return Task.CompletedTask;
        }

        public Task HandleAsync(EpisodeFileDeletedEvent message)
        {
            var episodeFile = message.EpisodeFile;

            if (message.Reason == DeleteMediaFileReason.NoLinkedEpisodes)
            {
                _logger.LogDebug("Removing episode file from DB as part of cleanup routine, not deleting extra files from disk.");
            }

            else
            {
                var series = _seriesService.GetSeries(message.EpisodeFile.SeriesId);

                foreach (var extra in _repository.GetFilesByEpisodeFile(episodeFile.Id))
                {
                    var path = Path.Combine(series.Path, extra.RelativePath);

                    if (_diskProvider.FileExists(path))
                    {
                        // Send to the recycling bin so they can be recovered if necessary
                        var subfolder = _diskProvider.GetParentFolder(series.Path).GetRelativePath(_diskProvider.GetParentFolder(path));
                        _recycleBinProvider.DeleteFile(path, subfolder);
                    }
                }
            }

            _logger.LogDebug("Deleting Extra from database for episode file: {EpisodeFile}", episodeFile);
            _repository.DeleteForEpisodeFile(episodeFile.Id);
            
            return Task.CompletedTask;
        }
    }
}
