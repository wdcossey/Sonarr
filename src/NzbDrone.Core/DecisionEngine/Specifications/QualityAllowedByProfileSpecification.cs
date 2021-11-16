using Microsoft.Extensions.Logging;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class QualityAllowedByProfileSpecification : IDecisionEngineSpecification
    {
        private readonly ILogger<QualityAllowedByProfileSpecification> _logger;

        public QualityAllowedByProfileSpecification(ILogger<QualityAllowedByProfileSpecification> logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            _logger.LogDebug("Checking if report meets quality requirements. {Quality}", subject.ParsedEpisodeInfo.Quality);

            var profile = subject.Series.QualityProfile.Value;
            var qualityIndex = profile.GetIndex(subject.ParsedEpisodeInfo.Quality.Quality);
            var qualityOrGroup = profile.Items[qualityIndex.Index];

            if (!qualityOrGroup.Allowed)
            {
                _logger.LogDebug("Quality {Quality} rejected by Series' quality profile", subject.ParsedEpisodeInfo.Quality);
                return Decision.Reject("{0} is not wanted in profile", subject.ParsedEpisodeInfo.Quality.Quality);
            }

            return Decision.Accept();
        }
    }
}
