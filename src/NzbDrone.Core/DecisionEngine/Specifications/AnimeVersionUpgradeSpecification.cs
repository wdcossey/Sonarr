using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class AnimeVersionUpgradeSpecification : IDecisionEngineSpecification
    {
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly ILogger<AnimeVersionUpgradeSpecification> _logger;

        public AnimeVersionUpgradeSpecification(UpgradableSpecification upgradableSpecification, ILogger<AnimeVersionUpgradeSpecification> logger)
        {
            _upgradableSpecification = upgradableSpecification;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            var releaseGroup = subject.ParsedEpisodeInfo.ReleaseGroup;

            if (subject.Series.SeriesType != SeriesTypes.Anime)
            {
                return Decision.Accept();
            }

            foreach (var file in subject.Episodes.Where(c => c.EpisodeFileId != 0).Select(c => c.EpisodeFile.Value))
            {
                if (file == null)
                {
                    continue;
                }

                if (_upgradableSpecification.IsRevisionUpgrade(file.Quality, subject.ParsedEpisodeInfo.Quality))
                {
                    if (file.ReleaseGroup.IsNullOrWhiteSpace())
                    {
                        _logger.LogDebug("Unable to compare release group, existing file's release group is unknown");
                        return Decision.Reject("Existing release group is unknown");
                    }

                    if (releaseGroup.IsNullOrWhiteSpace())
                    {
                        _logger.LogDebug("Unable to compare release group, release's release group is unknown");
                        return Decision.Reject("Release group is unknown");
                    }

                    if (file.ReleaseGroup != releaseGroup)
                    {
                        _logger.LogDebug("Existing Release group is: {FileReleaseGroup} - release's release group is: {ReleaseGroup}", file.ReleaseGroup, releaseGroup);
                        return Decision.Reject("{0} does not match existing release group {1}", releaseGroup, file.ReleaseGroup);
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
