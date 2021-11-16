using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class CutoffSpecification : IDecisionEngineSpecification
    {
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly IEpisodeFilePreferredWordCalculator _episodeFilePreferredWordCalculator;
        private readonly ILogger<CutoffSpecification> _logger;

        public CutoffSpecification(UpgradableSpecification upgradableSpecification,
                                   IEpisodeFilePreferredWordCalculator episodeFilePreferredWordCalculator,
                                   ILogger<CutoffSpecification> logger)
        {
            _upgradableSpecification = upgradableSpecification;
            _episodeFilePreferredWordCalculator = episodeFilePreferredWordCalculator;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            var qualityProfile = subject.Series.QualityProfile.Value;
            var languageProfile = subject.Series.LanguageProfile.Value;

            foreach (var file in subject.Episodes.Where(c => c.EpisodeFileId != 0).Select(c => c.EpisodeFile.Value))
            {
                if (file == null)
                {
                    _logger.LogDebug("File is no longer available, skipping this file.");
                    continue;
                }

                _logger.LogDebug("Comparing file quality and language with report. Existing file is {Quality} - {Language}", file.Quality, file.Language);

                if (!_upgradableSpecification.CutoffNotMet(qualityProfile,
                                                           languageProfile,
                                                           file.Quality,
                                                           file.Language,
                                                           _episodeFilePreferredWordCalculator.Calculate(subject.Series, file),
                                                           subject.ParsedEpisodeInfo.Quality,
                                                           subject.PreferredWordScore))
                {
                    _logger.LogDebug("Cutoff already met, rejecting.");

                    var qualityCutoffIndex = qualityProfile.GetIndex(qualityProfile.Cutoff);
                    var qualityCutoff = qualityProfile.Items[qualityCutoffIndex.Index];

                    return Decision.Reject("Existing file meets cutoff: {0} - {1}", qualityCutoff, languageProfile.Cutoff);
                }
            }

            return Decision.Accept();
        }
    }
}
