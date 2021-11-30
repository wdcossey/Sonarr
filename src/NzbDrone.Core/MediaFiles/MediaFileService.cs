using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;
using NzbDrone.Common;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileService
    {
        EpisodeFile Add(EpisodeFile episodeFile);
        EpisodeFile Update(EpisodeFile episodeFile);
        void Update(List<EpisodeFile> episodeFiles);
        void Delete(EpisodeFile episodeFile, DeleteMediaFileReason reason);
        List<EpisodeFile> GetFilesBySeries(int seriesId);
        List<EpisodeFile> GetFilesBySeason(int seriesId, int seasonNumber);
        List<EpisodeFile> GetFiles(IEnumerable<int> ids);
        List<EpisodeFile> GetFilesWithoutMediaInfo();
        List<string> FilterExistingFiles(List<string> files, Series series);
        EpisodeFile Get(int id);
        List<EpisodeFile> Get(IEnumerable<int> ids);
        List<EpisodeFile> GetFilesWithRelativePath(int seriesId, string relativePath);
    }

    public class MediaFileService : IMediaFileService, IHandleAsync<SeriesDeletedEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IMediaFileRepository _mediaFileRepository;

        public MediaFileService(IMediaFileRepository mediaFileRepository, IEventAggregator eventAggregator)
        {
            _mediaFileRepository = mediaFileRepository;
            _eventAggregator = eventAggregator;
        }

        public EpisodeFile Add(EpisodeFile episodeFile)
        {
            var addedFile = _mediaFileRepository.Insert(episodeFile);
            _eventAggregator.PublishEvent(new EpisodeFileAddedEvent(addedFile));
            return addedFile;
        }

        public EpisodeFile Update(EpisodeFile episodeFile)
        {
            return _mediaFileRepository.Update(episodeFile);
        }

        public void Update(List<EpisodeFile> episodeFiles)
        {
            _mediaFileRepository.UpdateMany(episodeFiles);
        }

        public void Delete(EpisodeFile episodeFile, DeleteMediaFileReason reason)
        {
            //Little hack so we have the episodes and series attached for the event consumers
            episodeFile.Episodes.LazyLoad();
            episodeFile.Path = Path.Combine(episodeFile.Series.Value.Path, episodeFile.RelativePath);

            _mediaFileRepository.Delete(episodeFile);
            _eventAggregator.PublishEvent(new EpisodeFileDeletedEvent(episodeFile, reason));
        }

        public List<EpisodeFile> GetFilesBySeries(int seriesId)
        {
            return _mediaFileRepository.GetFilesBySeries(seriesId);
        }

        public List<EpisodeFile> GetFilesBySeason(int seriesId, int seasonNumber)
        {
            return _mediaFileRepository.GetFilesBySeason(seriesId, seasonNumber);
        }

        public List<EpisodeFile> GetFiles(IEnumerable<int> ids)
        {
            return _mediaFileRepository.Get(ids).ToList();
        }

        public List<EpisodeFile> GetFilesWithoutMediaInfo()
        {
            return _mediaFileRepository.GetFilesWithoutMediaInfo();
        }

        public List<string> FilterExistingFiles(List<string> files, Series series)
        {
            var seriesFiles = GetFilesBySeries(series.Id).Select(f => Path.Combine(series.Path, f.RelativePath)).ToList();

            if (!seriesFiles.Any()) return files;

            return files.Except(seriesFiles, PathEqualityComparer.Instance).ToList();
        }

        public EpisodeFile Get(int id)
        {
            return _mediaFileRepository.Get(id);
        }

        public List<EpisodeFile> Get(IEnumerable<int> ids)
        {
            return _mediaFileRepository.Get(ids).ToList();
        }

        public List<EpisodeFile> GetFilesWithRelativePath(int seriesId, string relativePath)
        {
            return _mediaFileRepository.GetFilesWithRelativePath(seriesId, relativePath);
        }

        public Task HandleAsync(SeriesDeletedEvent message)
        {
            var files = GetFilesBySeries(message.Series.Id);
            _mediaFileRepository.DeleteMany(files);
            return Task.CompletedTask;
        }
    }
}
