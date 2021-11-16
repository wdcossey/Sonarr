using Microsoft.Extensions.Logging;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class ProtocolSpecification : IDecisionEngineSpecification
    {
        private readonly IDelayProfileService _delayProfileService;
        private readonly ILogger<ProtocolSpecification> _logger;

        public ProtocolSpecification(IDelayProfileService delayProfileService,
            ILogger<ProtocolSpecification> logger)
        {
            _delayProfileService = delayProfileService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            var delayProfile = _delayProfileService.BestForTags(subject.Series.Tags);

            if (subject.Release.DownloadProtocol == DownloadProtocol.Usenet && !delayProfile.EnableUsenet)
            {
                _logger.LogDebug("[{Title}] Usenet is not enabled for this series", subject.Release.Title);
                return Decision.Reject("Usenet is not enabled for this series");
            }

            if (subject.Release.DownloadProtocol == DownloadProtocol.Torrent && !delayProfile.EnableTorrent)
            {
                _logger.LogDebug("[{Title}] Torrent is not enabled for this series", subject.Release.Title);
                return Decision.Reject("Torrent is not enabled for this series");
            }

            return Decision.Accept();
        }
    }
}
