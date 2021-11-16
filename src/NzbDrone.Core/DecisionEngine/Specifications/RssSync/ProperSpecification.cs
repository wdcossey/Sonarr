using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class ProperSpecification : IDecisionEngineSpecification
    {
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly IConfigService _configService;
        private readonly ILogger<ProperSpecification> _logger;

        public ProperSpecification(UpgradableSpecification upgradableSpecification, IConfigService configService, ILogger<ProperSpecification> logger)
        {
            _upgradableSpecification = upgradableSpecification;
            _configService = configService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                return Decision.Accept();
            }

            var downloadPropersAndRepacks = _configService.DownloadPropersAndRepacks;

            if (downloadPropersAndRepacks == ProperDownloadTypes.DoNotPrefer)
            {
                _logger.LogDebug("Propers are not preferred, skipping check");
                return Decision.Accept();
            }

            foreach (var file in subject.Episodes.Where(c => c.EpisodeFileId != 0).Select(c => c.EpisodeFile.Value))
            {
                if (_upgradableSpecification.IsRevisionUpgrade(file.Quality, subject.ParsedEpisodeInfo.Quality))
                {
                    if (downloadPropersAndRepacks == ProperDownloadTypes.DoNotUpgrade)
                    {
                        _logger.LogDebug("Auto downloading of propers is disabled");
                        return Decision.Reject("Proper downloading is disabled");
                    }

                    if (file.DateAdded < DateTime.Today.AddDays(-7))
                    {
                        _logger.LogDebug("Proper for old file, rejecting: {Subject}", subject);
                        return Decision.Reject("Proper for old file");
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
