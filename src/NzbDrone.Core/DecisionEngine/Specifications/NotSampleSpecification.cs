using Microsoft.Extensions.Logging;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class NotSampleSpecification : IDecisionEngineSpecification
    {
        private readonly ILogger<NotSampleSpecification> _logger;

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public NotSampleSpecification(ILogger<NotSampleSpecification> logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (subject.Release.Title.ToLower().Contains("sample") && subject.Release.Size < 70.Megabytes())
            {
                _logger.LogDebug("Sample release, rejecting.");
                return Decision.Reject("Sample");
            }

            return Decision.Accept();
        }
    }
}
