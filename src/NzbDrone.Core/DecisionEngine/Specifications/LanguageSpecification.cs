using Microsoft.Extensions.Logging;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class LanguageSpecification : IDecisionEngineSpecification
    {
        private readonly ILogger<LanguageSpecification> _logger;

        public LanguageSpecification(ILogger<LanguageSpecification> logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            var wantedLanguage = subject.Series.LanguageProfile.Value.Languages;
            var _language = subject.ParsedEpisodeInfo.Language;

            _logger.LogDebug("Checking if report meets language requirements. {Language}", subject.ParsedEpisodeInfo.Language);

            if (!wantedLanguage.Exists(v => v.Allowed && v.Language == _language))
            {
                _logger.LogDebug("Report Language: {Language} rejected because it is not wanted in profile {Name}", _language, subject.Series.LanguageProfile.Value.Name);
                return Decision.Reject("{0} is not allowed in profile {1}", _language, subject.Series.LanguageProfile.Value.Name);
            }

            return Decision.Accept();
        }
    }
}
