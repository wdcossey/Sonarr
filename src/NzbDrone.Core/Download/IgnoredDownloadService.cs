using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Download
{
    public interface IIgnoredDownloadService
    {
        bool IgnoreDownload(TrackedDownload trackedDownload);
    }

    public class IgnoredDownloadService : IIgnoredDownloadService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<IgnoredDownloadService> _logger;

        public IgnoredDownloadService(IEventAggregator eventAggregator,
                                      ILogger<IgnoredDownloadService> logger)
        {
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public bool IgnoreDownload(TrackedDownload trackedDownload)
        {
            var series = trackedDownload.RemoteEpisode.Series;

            if (series == null)
            {
                _logger.LogDebug("Unable to ignore download for unknown series");
                return false;
            }

            var episodes = trackedDownload.RemoteEpisode.Episodes;

            var downloadIgnoredEvent = new DownloadIgnoredEvent
                                      {
                                          SeriesId = series.Id,
                                          EpisodeIds = episodes.Select(e => e.Id).ToList(),
                                          Language = trackedDownload.RemoteEpisode.ParsedEpisodeInfo.Language,
                                          Quality = trackedDownload.RemoteEpisode.ParsedEpisodeInfo.Quality,
                                          SourceTitle = trackedDownload.DownloadItem.Title,
                                          DownloadClientInfo = trackedDownload.DownloadItem.DownloadClientInfo,
                                          DownloadId = trackedDownload.DownloadItem.DownloadId,
                                          Message = "Manually ignored"
                                      };

            _eventAggregator.PublishEvent(downloadIgnoredEvent);
            return true;
        }
    }
}
