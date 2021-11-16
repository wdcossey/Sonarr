using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class SceneMappingSpecification : IDecisionEngineSpecification
    {
        private readonly ILogger<SceneMappingSpecification> _logger;

        public SceneMappingSpecification(ILogger<SceneMappingSpecification> logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Temporary; // Temporary till there's a mapping

        public Decision IsSatisfiedBy(RemoteEpisode remoteEpisode, SearchCriteriaBase searchCriteria)
        {
            if (remoteEpisode.SceneMapping == null)
            {
                _logger.LogDebug("No applicable scene mapping, skipping.");
                return Decision.Accept();
            }

            if (remoteEpisode.SceneMapping.SceneOrigin.IsNullOrWhiteSpace())
            {
                _logger.LogDebug("No explicit scene origin in scene mapping.");
                return Decision.Accept();
            }


            var split = remoteEpisode.SceneMapping.SceneOrigin.Split(':');

            var isInteractive = (searchCriteria != null && searchCriteria.InteractiveSearch);

            if (remoteEpisode.SceneMapping.Comment.IsNotNullOrWhiteSpace())
            {
                _logger.LogDebug("SceneMapping has origin {SceneOrigin} with comment '{Comment}'.", remoteEpisode.SceneMapping.SceneOrigin, remoteEpisode.SceneMapping.Comment);
            }
            else
            {
                _logger.LogDebug("SceneMapping has origin {SceneOrigin}.", remoteEpisode.SceneMapping.SceneOrigin);
            }

            if (split[0] == "mixed")
            {
                _logger.LogDebug("SceneMapping origin is explicitly mixed, this means these were released with multiple unidentifiable numbering schemes.");

                if (remoteEpisode.SceneMapping.Comment.IsNotNullOrWhiteSpace())
                {
                    return Decision.Reject("{0} has ambiguous numbering");
                }
                else
                {
                    return Decision.Reject("Ambiguous numbering");
                }
            }

            if (split[0] == "unknown")
            {
                var type = split.Length >= 2 ? split[1] : "scene";

                _logger.LogDebug("SceneMapping origin is explicitly unknown, unsure what numbering scheme it uses but '{Type}' will be assumed. Provide full release title to Sonarr/TheXEM team.", type);
            }

            return Decision.Accept();
        }
    }
}
