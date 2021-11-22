using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Download
{
    public class RedownloadFailedDownloadService : IHandleAsync<DownloadFailedEvent>
    {
        private readonly IConfigService _configService;
        private readonly IEpisodeService _episodeService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly ILogger<RedownloadFailedDownloadService> _logger;

        public RedownloadFailedDownloadService(IConfigService configService,
                                               IEpisodeService episodeService,
                                               IManageCommandQueue commandQueueManager,
                                               ILogger<RedownloadFailedDownloadService> logger)
        {
            _configService = configService;
            _episodeService = episodeService;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        [EventHandleOrder(EventHandleOrder.Last)]
        public Task HandleAsync(DownloadFailedEvent message)
        {
            if (!_configService.AutoRedownloadFailed)
            {
                _logger.LogDebug("Auto redownloading failed episodes is disabled");
                return Task.CompletedTask;
            }

            if (message.EpisodeIds.Count == 1)
            {
                _logger.LogDebug("Failed download only contains one episode, searching again");

                _commandQueueManager.Push(new EpisodeSearchCommand(message.EpisodeIds));

                return Task.CompletedTask;
            }

            var seasonNumber = _episodeService.GetEpisode(message.EpisodeIds.First()).SeasonNumber;
            var episodesInSeason = _episodeService.GetEpisodesBySeason(message.SeriesId, seasonNumber);

            if (message.EpisodeIds.Count == episodesInSeason.Count)
            {
                _logger.LogDebug("Failed download was entire season, searching again");

                _commandQueueManager.Push(new SeasonSearchCommand
                {
                    SeriesId = message.SeriesId,
                    SeasonNumber = seasonNumber
                });

                return Task.CompletedTask;
            }

            _logger.LogDebug("Failed download contains multiple episodes, probably a double episode, searching again");

            _commandQueueManager.Push(new EpisodeSearchCommand(message.EpisodeIds));
            
            return Task.CompletedTask;
        }
    }
}
