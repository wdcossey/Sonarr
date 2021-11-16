using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Specifications
{
    public class UnverifiedSceneNumberingSpecification : IImportDecisionEngineSpecification
    {
        private readonly ILogger<UnverifiedSceneNumberingSpecification> _logger;

        public UnverifiedSceneNumberingSpecification(ILogger<UnverifiedSceneNumberingSpecification> logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalEpisode localEpisode, DownloadClientItem downloadClientItem)
        {
            if (localEpisode.ExistingFile)
            {
                _logger.LogDebug("Skipping scene numbering check for existing episode");
                return Decision.Accept();
            }

            if (localEpisode.Episodes.Any(v => v.UnverifiedSceneNumbering))
            {
                _logger.LogDebug("This file uses unverified scene numbers, will not auto-import until numbering is confirmed on TheXEM. Skipping {Path}", localEpisode.Path);
                return Decision.Reject("This show has individual episode mappings on TheXEM but the mapping for this episode has not been confirmed yet by their administrators. TheXEM needs manual input.");
            }

            return Decision.Accept();
        }
    }
}
