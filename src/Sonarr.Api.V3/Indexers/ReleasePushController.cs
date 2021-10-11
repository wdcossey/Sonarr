using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Languages;
using NzbDrone.Core.Profiles.Qualities;

namespace Sonarr.Api.V3.Indexers
{
    [ApiController]
    [SonarrApiRoute("release", RouteVersion.V3)]
    public class ReleasePushController : ReleaseControllerBase
    {
        private readonly IMakeDownloadDecision _downloadDecisionMaker;
        private readonly IProcessDownloadDecisions _downloadDecisionProcessor;
        private readonly IIndexerFactory _indexerFactory;
        private readonly ILogger<ReleasePushController> _logger;

        public ReleasePushController(
            IMakeDownloadDecision downloadDecisionMaker,
            IProcessDownloadDecisions downloadDecisionProcessor,
            IIndexerFactory indexerFactory,
            ILanguageProfileService languageProfileService,
            IQualityProfileService qualityProfileService,
            ILogger<ReleasePushController> logger)
            : base(languageProfileService, qualityProfileService)
        {
            _downloadDecisionMaker = downloadDecisionMaker;
            _downloadDecisionProcessor = downloadDecisionProcessor;
            _indexerFactory = indexerFactory;
            _logger = logger;

            /*PostValidator.RuleFor(s => s.Title).NotEmpty();
            PostValidator.RuleFor(s => s.DownloadUrl).NotEmpty();
            PostValidator.RuleFor(s => s.Protocol).NotEmpty();
            PostValidator.RuleFor(s => s.PublishDate).NotEmpty();*/
        }

        [HttpPost("push")]
        public IActionResult ProcessRelease([FromBody] ReleaseResource resource)
        {
            _logger.LogInformation("Release pushed: {Title} - {DownloadUrl}", resource.Title, resource.DownloadUrl);

            var info = resource.ToModel();

            info.Guid = "PUSH-" + info.DownloadUrl;

            ResolveIndexer(info);

            var decisions = _downloadDecisionMaker.GetRssDecision(new List<ReleaseInfo> { info });
            _downloadDecisionProcessor.ProcessDecisions(decisions);

            var firstDecision = decisions.FirstOrDefault();

            if (firstDecision?.RemoteEpisode.ParsedEpisodeInfo == null)
            {
                throw new ValidationException(new List<ValidationFailure>{ new ValidationFailure("Title", "Unable to parse", resource.Title) });
            }

            return Ok(MapDecisions(new [] { firstDecision }));
        }

        private void ResolveIndexer(ReleaseInfo release)
        {
            if (release.IndexerId == 0 && release.Indexer.IsNotNullOrWhiteSpace())
            {
                var indexer = _indexerFactory.All().FirstOrDefault(v => v.Name == release.Indexer);
                if (indexer != null)
                {
                    release.IndexerId = indexer.Id;
                    _logger.LogDebug("Push Release {Title} associated with indexer {IndexerId} - {Indexer}.", release.Title, release.IndexerId, release.Indexer);
                }
                else
                {
                    _logger.LogDebug("Push Release {Title} not associated with known indexer {Indexer}.", release.Title, release.Indexer);
                }
            }
            else if (release.IndexerId != 0 && release.Indexer.IsNullOrWhiteSpace())
            {
                try
                {
                    var indexer = _indexerFactory.Get(release.IndexerId);
                    release.Indexer = indexer.Name;
                    _logger.LogDebug("Push Release {Title} associated with indexer {IndexerId} - {Indexer}.", release.Title, release.IndexerId, release.Indexer);
                }
                catch (ModelNotFoundException)
                {
                    _logger.LogDebug("Push Release {Title} not associated with known indexer {IndexerId}.", release.Title, release.IndexerId);
                    release.IndexerId = 0;
                }
            }
            else
            {
                _logger.LogDebug("Push Release {Title} not associated with an indexer.", release.Title);
            }
        }
    }
}
