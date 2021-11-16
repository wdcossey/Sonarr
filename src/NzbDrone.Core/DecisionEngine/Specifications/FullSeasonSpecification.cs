using System;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Common.Extensions;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class FullSeasonSpecification : IDecisionEngineSpecification
    {
        private readonly ILogger<FullSeasonSpecification> _logger;

        public FullSeasonSpecification(ILogger<FullSeasonSpecification> logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (subject.ParsedEpisodeInfo.FullSeason)
            {
                _logger.LogDebug("Checking if all episodes in full season release have aired. {Title}", subject.Release.Title);

                if (subject.Episodes.Any(e => !e.AirDateUtc.HasValue || e.AirDateUtc.Value.After(DateTime.UtcNow)))
                {
                    _logger.LogDebug("Full season release {Title} rejected. All episodes haven't aired yet.", subject.Release.Title);
                    return Decision.Reject("Full season release rejected. All episodes haven't aired yet.");
                }
            }

            return Decision.Accept();
        }
    }
}
