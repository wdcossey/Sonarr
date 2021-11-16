using System;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class MinimumAgeSpecification : IDecisionEngineSpecification
    {
        private readonly IConfigService _configService;
        private readonly ILogger<MinimumAgeSpecification> _logger;

        public MinimumAgeSpecification(IConfigService configService, ILogger<MinimumAgeSpecification> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Temporary;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (subject.Release.DownloadProtocol != Indexers.DownloadProtocol.Usenet)
            {
                _logger.LogDebug("Not checking minimum age requirement for non-usenet report");
                return Decision.Accept();
            }

            var age = subject.Release.AgeMinutes;
            var minimumAge = _configService.MinimumAge;
            var ageRounded = Math.Round(age, 1);

            if (minimumAge == 0)
            {
                _logger.LogDebug("Minimum age is not set.");
                return Decision.Accept();
            }


            _logger.LogDebug("Checking if report meets minimum age requirements. {AgeRounded}", ageRounded);

            if (age < minimumAge)
            {
                _logger.LogDebug("Only {AgeRounded} minutes old, minimum age is {MinimumAge} minutes", ageRounded, minimumAge);
                return Decision.Reject("Only {0} minutes old, minimum age is {1} minutes", ageRounded, minimumAge);
            }

            _logger.LogDebug("Release is {AgeRounded} minutes old, greater than minimum age of {MinimumAge} minutes", ageRounded, minimumAge);

            return Decision.Accept();
        }
    }
}
