using Microsoft.Extensions.Logging;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Specifications
{
    public class FullSeasonSpecification : IImportDecisionEngineSpecification
    {
        private readonly ILogger<FullSeasonSpecification> _logger;

        public FullSeasonSpecification(ILogger<FullSeasonSpecification> logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalEpisode localEpisode, DownloadClientItem downloadClientItem)
        {
            if (localEpisode.FileEpisodeInfo == null)
            {
                return Decision.Accept();
            }

            if (localEpisode.FileEpisodeInfo.FullSeason)
            {
                _logger.LogDebug("Single episode file detected as containing all episodes in the season");
                return Decision.Reject("Single episode file contains all episodes in seasons");
            }

            return Decision.Accept();
        }
    }
}
