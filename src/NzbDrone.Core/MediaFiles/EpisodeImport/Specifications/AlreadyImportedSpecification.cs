using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Specifications
{
    public class AlreadyImportedSpecification : IImportDecisionEngineSpecification
    {
        private readonly IHistoryService _historyService;
        private readonly ILogger<AlreadyImportedSpecification> _logger;

        public AlreadyImportedSpecification(IHistoryService historyService,
                                            ILogger<AlreadyImportedSpecification> logger)
        {
            _historyService = historyService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Database;

        public Decision IsSatisfiedBy(LocalEpisode localEpisode, DownloadClientItem downloadClientItem)
        {
            if (downloadClientItem == null)
            {
                _logger.LogDebug("No download client information is available, skipping");
                return Decision.Accept();
            }

            foreach (var episode in localEpisode.Episodes)
            {
                if (!episode.HasFile)
                {
                    _logger.LogDebug("Skipping already imported check for episode without file");
                    continue;
                }

                var episodeHistory = _historyService.FindByEpisodeId(episode.Id);
                var lastImported = episodeHistory.FirstOrDefault(h => h.EventType == EpisodeHistoryEventType.DownloadFolderImported);
                var lastGrabbed = episodeHistory.FirstOrDefault(h => h.EventType == EpisodeHistoryEventType.Grabbed);

                if (lastImported == null)
                {
                    _logger.LogTrace("Episode file has not been imported");
                    continue;
                }

                // If the release was grabbed again after importing don't reject it
                if (lastGrabbed != null && lastGrabbed.Date.After(lastImported.Date))
                {
                    _logger.LogTrace("Episode file was grabbed again after importing");
                    continue;
                }

                if (lastImported.DownloadId == downloadClientItem.DownloadId)
                {
                    _logger.LogDebug("Episode file previously imported at {Date}", lastImported.Date);
                    return Decision.Reject("Episode file already imported at {0}", lastImported.Date);
                }
            }

            return Decision.Accept();
        }
    }
}
