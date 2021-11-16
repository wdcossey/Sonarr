using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class DeletedEpisodeFileSpecification : IDecisionEngineSpecification
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly ILogger<DeletedEpisodeFileSpecification> _logger;

        public DeletedEpisodeFileSpecification(IDiskProvider diskProvider, IConfigService configService, ILogger<DeletedEpisodeFileSpecification> logger)
        {
            _diskProvider = diskProvider;
            _configService = configService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Disk;
        public RejectionType Type => RejectionType.Temporary;

        public virtual Decision IsSatisfiedBy(RemoteEpisode subject, SearchCriteriaBase searchCriteria)
        {
            if (!_configService.AutoUnmonitorPreviouslyDownloadedEpisodes)
            {
                return Decision.Accept();
            }

            if (searchCriteria != null)
            {
                _logger.LogDebug("Skipping deleted episodefile check during search");
                return Decision.Accept();
            }

            var missingEpisodeFiles = subject.Episodes
                                             .Where(v => v.EpisodeFileId != 0)
                                             .Select(v => v.EpisodeFile.Value)
                                             .DistinctBy(v => v.Id)
                                             .Where(v => IsEpisodeFileMissing(subject.Series, v))
                                             .ToArray();

            if (missingEpisodeFiles.Any())
            {
                foreach (var missingEpisodeFile in missingEpisodeFiles)
                {
                    _logger.LogTrace("Episode file {RelativePath} is missing from disk.", missingEpisodeFile.RelativePath);
                }

                _logger.LogDebug("Files for this episode exist in the database but not on disk, will be unmonitored on next diskscan. skipping.");
                return Decision.Reject("Series is not monitored");
            }

            return Decision.Accept();
        }

        private bool IsEpisodeFileMissing(Series series, EpisodeFile episodeFile)
        {
            var fullPath = Path.Combine(series.Path, episodeFile.RelativePath);

            return !_diskProvider.FileExists(fullPath);
        }
    }
}
