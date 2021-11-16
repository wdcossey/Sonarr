using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Releases;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class ReleaseRestrictionsSpecification : IDecisionEngineSpecification
    {
        private readonly ILogger<ReleaseRestrictionsSpecification> _logger;
        private readonly IReleaseProfileService _releaseProfileService;
        private readonly ITermMatcherService _termMatcherService;

        public ReleaseRestrictionsSpecification(ITermMatcherService termMatcherService, IReleaseProfileService releaseProfileService, ILogger<ReleaseRestrictionsSpecification> logger)
        {
            _logger = logger;
            _releaseProfileService = releaseProfileService;
            _termMatcherService = termMatcherService;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            _logger.LogDebug("Checking if release meets restrictions: {Subject}", subject);

            var title = subject.Release.Title;
            var releaseProfiles = _releaseProfileService.EnabledForTags(subject.Series.Tags, subject.Release.IndexerId);

            var required = releaseProfiles.Where(r => r.Required.Any());
            var ignored = releaseProfiles.Where(r => r.Ignored.Any());

            foreach (var r in required)
            {
                var requiredTerms = r.Required;

                var foundTerms = ContainsAny(requiredTerms, title);
                if (foundTerms.Empty())
                {
                    var terms = string.Join(", ", requiredTerms);
                    _logger.LogDebug("[{Title}] does not contain one of the required terms: {Terms}", title, terms);
                    return Decision.Reject("Does not contain one of the required terms: {0}", terms);
                }
            }

            foreach (var r in ignored)
            {
                var ignoredTerms = r.Ignored;

                var foundTerms = ContainsAny(ignoredTerms, title);
                if (foundTerms.Any())
                {
                    var terms = string.Join(", ", foundTerms);
                    _logger.LogDebug("[{Title}] contains these ignored terms: {Terms}", title, terms);
                    return Decision.Reject("Contains these ignored terms: {0}", terms);
                }
            }

            _logger.LogDebug("[{Subject}] No restrictions apply, allowing", subject);
            return Decision.Accept();
        }

        private List<string> ContainsAny(List<string> terms, string title)
        {
            return terms.Where(t => _termMatcherService.IsMatch(t, title)).ToList();
        }
    }
}
