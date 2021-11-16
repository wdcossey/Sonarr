using Microsoft.Extensions.Logging;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class SeriesSpecification : IDecisionEngineSpecification
    {
        private readonly ILogger<SeriesSpecification> _logger;

        public SeriesSpecification(ILogger<SeriesSpecification> logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public Decision IsSatisfiedBy(RemoteEpisode remoteEpisode, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria == null)
            {
                return Decision.Accept();
            }

            _logger.LogDebug("Checking if series matches searched series");

            if (remoteEpisode.Series.Id != searchCriteria.Series.Id)
            {
                _logger.LogDebug("Series {RemoteSeries} does not match {SearchSeries}", remoteEpisode.Series, searchCriteria.Series);
                return Decision.Reject("Wrong series");
            }

            return Decision.Accept();
        }
    }
}
