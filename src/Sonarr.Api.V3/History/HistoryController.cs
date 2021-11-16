using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using Sonarr.Api.V3.Episodes;
using Sonarr.Api.V3.Series;
using Sonarr.Http;
using Sonarr.Http.Attributes;
using Sonarr.Http.Extensions;
using Sonarr.Http.ModelBinders;

namespace Sonarr.Api.V3.History
{
    [ApiController]
    [SonarrApiRoute("history", RouteVersion.V3)]
    public class HistoryController : ControllerBase
    {
        private readonly IHistoryService _historyService;
        private readonly IUpgradableSpecification _upgradableSpecification;
        private readonly IFailedDownloadService _failedDownloadService;

        public HistoryController(
            IHistoryService historyService,
            IUpgradableSpecification upgradableSpecification,
            IFailedDownloadService failedDownloadService)
        {
            _historyService = historyService;
            _upgradableSpecification = upgradableSpecification;
            _failedDownloadService = failedDownloadService;
        }

        [ProducesResponseType(typeof(PagingResource<HistoryResource>), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult GetHistory(
            [FromQuery] [ModelBinder(typeof(PagingResourceModelBinder))] PagingResource<HistoryResource> pagingResource,
            [FromQuery] bool includeSeries = false,
            [FromQuery] bool includeEpisode = false)
        {
            var pagingSpec = pagingResource.MapToPagingSpec<HistoryResource, EpisodeHistory>("date", SortDirection.Descending);

            var eventTypeFilter = pagingResource.Filters?.FirstOrDefault(f => f.Key == "eventType");
            var episodeIdFilter = pagingResource.Filters?.FirstOrDefault(f => f.Key == "episodeId");
            var downloadIdFilter = pagingResource.Filters?.FirstOrDefault(f => f.Key == "downloadId");

            if (eventTypeFilter != null)
            {
                var filterValue = (EpisodeHistoryEventType)Convert.ToInt32(eventTypeFilter.Value);
                pagingSpec.FilterExpressions.Add(v => v.EventType == filterValue);
            }

            if (episodeIdFilter != null)
            {
                var episodeId = Convert.ToInt32(episodeIdFilter.Value);
                pagingSpec.FilterExpressions.Add(h => h.EpisodeId == episodeId);
            }

            if (downloadIdFilter != null)
            {
                var downloadId = downloadIdFilter.Value;
                pagingSpec.FilterExpressions.Add(h => h.DownloadId == downloadId);
            }

            return Ok(pagingSpec.ApplyToPage(_historyService.Paged, h => MapToResource(h, includeSeries, includeEpisode)));
        }

        [ProducesResponseType(typeof(IEnumerable<HistoryResource>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("since")]
        public IActionResult GetHistorySince(
            [FromQuery] DateTime? date = null,
            [FromQuery] EpisodeHistoryEventType? eventType = null,
            [FromQuery] bool includeSeries = false,
            [FromQuery] bool includeEpisode = false)
        {
            if (!date.HasValue)
                return BadRequest($"{nameof(date)} is missing");

            return Ok(_historyService.Since(date.Value, eventType).Select(h => MapToResource(h, includeSeries, includeEpisode)));
        }

        [ProducesResponseType(typeof(IEnumerable<HistoryResource>), StatusCodes.Status200OK)]
        [HttpGet("series")]
        public IActionResult GetSeriesHistory(
            [FromQuery] int? seriesId = null,
            [FromQuery] int? seasonNumber = null,
            [FromQuery] EpisodeHistoryEventType? eventType = null,
            [FromQuery] bool includeSeries = false,
            [FromQuery] bool includeEpisode = false)
        {
            if (!seriesId.HasValue)
                return BadRequest("seriesId is missing");

            return Ok(seasonNumber.HasValue
                ? _historyService.GetBySeason(seriesId.Value, seasonNumber.Value, eventType).Select(h => MapToResource(h, includeSeries, includeEpisode))
                : _historyService.GetBySeries(seriesId.Value, eventType).Select(h => MapToResource(h, includeSeries, includeEpisode)));
        }

        // v4 TODO: Getting the ID from the form is atypical, consider removing.
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost("failed")]
        public IActionResult MarkAsFailedFromForm([FromForm] int id)
            => MarkAsFailed(id);

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpPost("failed/{id:int:required}")]
        public IActionResult MarkAsFailed(int id)
        {
            _failedDownloadService.MarkAsFailed(id);
            return Ok(new object());
        }

        private HistoryResource MapToResource(EpisodeHistory model, bool includeSeries, bool includeEpisode)
        {
            var resource = model.ToResource();

            if (includeSeries)
                resource.Series = model.Series.ToResource();

            if (includeEpisode)
                resource.Episode = model.Episode.ToResource();

            if (model.Series != null)
            {
                resource.QualityCutoffNotMet = _upgradableSpecification.QualityCutoffNotMet(model.Series.QualityProfile.Value, model.Quality);
                resource.LanguageCutoffNotMet = _upgradableSpecification.LanguageCutoffNotMet(model.Series.LanguageProfile, model.Language);
            }

            return resource;
        }
    }
}
