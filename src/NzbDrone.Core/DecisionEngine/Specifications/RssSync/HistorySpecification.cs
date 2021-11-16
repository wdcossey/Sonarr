using System;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.History;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Releases;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class HistorySpecification : IDecisionEngineSpecification
    {
        private readonly IHistoryService _historyService;
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly IConfigService _configService;
        private readonly IPreferredWordService _preferredWordServiceCalculator;
        private readonly ILogger<HistorySpecification> _logger;

        public HistorySpecification(IHistoryService historyService,
                                           UpgradableSpecification upgradableSpecification,
                                           IConfigService configService,
                                           IPreferredWordService preferredWordServiceCalculator,
                                           ILogger<HistorySpecification> logger)
        {
            _historyService = historyService;
            _upgradableSpecification = upgradableSpecification;
            _configService = configService;
            _preferredWordServiceCalculator = preferredWordServiceCalculator;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Database;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                _logger.LogDebug("Skipping history check during search");
                return Decision.Accept();
            }

            var cdhEnabled = _configService.EnableCompletedDownloadHandling;

            _logger.LogDebug("Performing history status check on report");
            foreach (var episode in subject.Episodes)
            {
                _logger.LogDebug("Checking current status of episode [{Id}] in history", episode.Id);
                var mostRecent = _historyService.MostRecentForEpisode(episode.Id);

                if (mostRecent != null && mostRecent.EventType == EpisodeHistoryEventType.Grabbed)
                {
                    var recent = mostRecent.Date.After(DateTime.UtcNow.AddHours(-12));

                    if (!recent && cdhEnabled)
                    {
                        continue;
                    }

                    // The series will be the same as the one in history since it's the same episode.
                    // Instead of fetching the series from the DB reuse the known series.
                    var preferredWordScore = _preferredWordServiceCalculator.Calculate(subject.Series, mostRecent.SourceTitle, subject.Release?.IndexerId ?? 0);

                    var cutoffUnmet = _upgradableSpecification.CutoffNotMet(
                        subject.Series.QualityProfile,
                        subject.Series.LanguageProfile,
                        mostRecent.Quality,
                        mostRecent.Language,
                        preferredWordScore,
                        subject.ParsedEpisodeInfo.Quality,
                        subject.PreferredWordScore);

                    var upgradeable = _upgradableSpecification.IsUpgradable(
                        subject.Series.QualityProfile,
                        subject.Series.LanguageProfile,
                        mostRecent.Quality,
                        mostRecent.Language,
                        preferredWordScore,
                        subject.ParsedEpisodeInfo.Quality,
                        subject.ParsedEpisodeInfo.Language,
                        subject.PreferredWordScore);

                    if (!cutoffUnmet)
                    {
                        if (recent)
                        {
                            return Decision.Reject("Recent grab event in history already meets cutoff: {0}", mostRecent.Quality);
                        }

                        return Decision.Reject("CDH is disabled and grab event in history already meets cutoff: {0}", mostRecent.Quality);
                    }

                    if (!upgradeable)
                    {
                        if (recent)
                        {
                            return Decision.Reject("Recent grab event in history is of equal or higher quality: {0}", mostRecent.Quality);
                        }

                        return Decision.Reject("CDH is disabled and grab event in history is of equal or higher quality: {0}", mostRecent.Quality);
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
