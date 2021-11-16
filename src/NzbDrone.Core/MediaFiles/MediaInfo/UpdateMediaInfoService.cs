using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.MediaFiles.MediaInfo
{
    public interface IUpdateMediaInfo
    {
        void Update(EpisodeFile episodeFile, Series series);
    }

    public class UpdateMediaInfoService : IHandle<SeriesScannedEvent>, IUpdateMediaInfo
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IVideoFileInfoReader _videoFileInfoReader;
        private readonly IConfigService _configService;
        private readonly ILogger<UpdateMediaInfoService> _logger;

        public UpdateMediaInfoService(IDiskProvider diskProvider,
                                      IMediaFileService mediaFileService,
                                      IVideoFileInfoReader videoFileInfoReader,
                                      IConfigService configService,
                                      ILogger<UpdateMediaInfoService> logger)
        {
            _diskProvider = diskProvider;
            _mediaFileService = mediaFileService;
            _videoFileInfoReader = videoFileInfoReader;
            _configService = configService;
            _logger = logger;
        }

        public void Handle(SeriesScannedEvent message)
        {
            if (!_configService.EnableMediaInfo)
            {
                _logger.LogDebug("MediaInfo is disabled");
                return;
            }

            var allMediaFiles = _mediaFileService.GetFilesBySeries(message.Series.Id);
            var filteredMediaFiles = allMediaFiles.Where(c =>
                c.MediaInfo == null ||
                c.MediaInfo.SchemaRevision < VideoFileInfoReader.MINIMUM_MEDIA_INFO_SCHEMA_REVISION).ToList();

            foreach (var mediaFile in filteredMediaFiles)
            {
                UpdateMediaInfo(mediaFile, message.Series);
            }
        }

        public void Update(EpisodeFile episodeFile, Series series)
        {
            if (!_configService.EnableMediaInfo)
            {
                _logger.LogDebug("MediaInfo is disabled");
                return;
            }
            UpdateMediaInfo(episodeFile, series);
        }

        private void UpdateMediaInfo(EpisodeFile episodeFile, Series series)
        {
            var path = Path.Combine(series.Path, episodeFile.RelativePath);

            if (!_diskProvider.FileExists(path))
            {
                _logger.LogDebug("Can't update MediaInfo because '{Path}' does not exist", path);
                return;
            }

            var updatedMediaInfo = _videoFileInfoReader.GetMediaInfo(path);

            if (updatedMediaInfo != null)
            {
                episodeFile.MediaInfo = updatedMediaInfo;
                _mediaFileService.Update(episodeFile);
                _logger.LogDebug("Updated MediaInfo for '{Path}'", path);
            }
        }
    }
}
