﻿using Microsoft.Extensions.Logging;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class RetentionSpecification : IDecisionEngineSpecification
    {
        private readonly IConfigService _configService;
        private readonly ILogger<RetentionSpecification> _logger;

        public RetentionSpecification(IConfigService configService, ILogger<RetentionSpecification> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (subject.Release.DownloadProtocol != Indexers.DownloadProtocol.Usenet)
            {
                _logger.LogDebug("Not checking retention requirement for non-usenet report");
                return Decision.Accept();
            }

            var age = subject.Release.Age;
            var retention = _configService.Retention;

            _logger.LogDebug("Checking if report meets retention requirements. {Age}", age);
            if (retention > 0 && age > retention)
            {
                _logger.LogDebug("Report age: {Age} rejected by user's retention limit", age);
                return Decision.Reject("Older than configured retention");
            }

            return Decision.Accept();
        }
    }
}
