using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.History;

namespace NzbDrone.Core.Download.TrackedDownloads
{
    public interface ITrackedDownloadAlreadyImported
    {
        bool IsImported(TrackedDownload trackedDownload, List<EpisodeHistory> historyItems);
    }

    public class TrackedDownloadAlreadyImported : ITrackedDownloadAlreadyImported
    {
        private readonly ILogger<TrackedDownloadAlreadyImported> _logger;

        public TrackedDownloadAlreadyImported(ILogger<TrackedDownloadAlreadyImported> logger)
        {
            _logger = logger;
        }

        public bool IsImported(TrackedDownload trackedDownload, List<EpisodeHistory> historyItems)
        {
            _logger.LogTrace("Checking if all episodes for '{Title}' have been imported", trackedDownload.DownloadItem.Title);

            if (historyItems.Empty())
            {
                _logger.LogTrace("No history for {Title}", trackedDownload.DownloadItem.Title);
                return false;
            }

            var allEpisodesImportedInHistory = trackedDownload.RemoteEpisode.Episodes.All(e =>
            {
                var lastHistoryItem = historyItems.FirstOrDefault(h => h.EpisodeId == e.Id);

                if (lastHistoryItem == null)
                {
                    _logger.LogTrace("No history for episode: S{SeasonNumber:00}E{EpisodeNumber:00} [{Id}]", e.SeasonNumber, e.EpisodeNumber, e.Id);
                    return false;
                }

                _logger.LogTrace("Last event for episode: S{SeasonNumber:00}E{EpisodeNumber:00} [{Id}] is: {EventType}", e.SeasonNumber, e.EpisodeNumber, e.Id, lastHistoryItem.EventType);

                return lastHistoryItem.EventType == EpisodeHistoryEventType.DownloadFolderImported;
            });

            _logger.LogTrace("All episodes for '{Title}' have been imported: {AllEpisodesImportedInHistory}", trackedDownload.DownloadItem.Title, allEpisodesImportedInHistory);

            return allEpisodesImportedInHistory;
        }
    }
}
