using Microsoft.Extensions.Logging;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class SameEpisodesGrabSpecification : IDecisionEngineSpecification
    {
        private readonly SameEpisodesSpecification _sameEpisodesSpecification;
        private readonly ILogger<SameEpisodesGrabSpecification> _logger;

        public SameEpisodesGrabSpecification(SameEpisodesSpecification sameEpisodesSpecification, ILogger<SameEpisodesGrabSpecification> logger)
        {
            _sameEpisodesSpecification = sameEpisodesSpecification;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (_sameEpisodesSpecification.IsSatisfiedBy(subject.Episodes))
            {
                return Decision.Accept();
            }

            _logger.LogDebug("Episode file on disk contains more episodes than this release contains");
            return Decision.Reject("Episode file on disk contains more episodes than this release contains");
        }
    }
}
