﻿using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Tv;
using Sonarr.Api.V3.Episodes;
using Sonarr.Http;
using Sonarr.Http.Attributes;
using Sonarr.Http.Extensions;
using Sonarr.Http.ModelBinders;

namespace Sonarr.Api.V3.Wanted
{
    [ApiController]
    [SonarrApiRoute("wanted/missing", RouteVersion.V3)]
    public class MissingController : EpisodeControllerBase
    {
        public MissingController(IEpisodeService episodeService,
            ISeriesService seriesService,
            IUpgradableSpecification upgradableSpecification)
            : base(episodeService, seriesService, upgradableSpecification) { }

        [HttpGet]
        public IActionResult GetMissingEpisodes(
            [FromQuery] [ModelBinder(typeof(PagingResourceModelBinder))] PagingResource<EpisodeResource> pagingResource,
            [FromQuery] bool includeSeries = false,
            [FromQuery] bool includeImages = false)
        {
            var pagingSpec = new PagingSpec<Episode>
            {
                Page = pagingResource.Page,
                PageSize = pagingResource.PageSize,
                SortKey = pagingResource.SortKey,
                SortDirection = pagingResource.SortDirection
            };

            var monitoredFilter = pagingResource.Filters.FirstOrDefault(f => f.Key == "monitored");

            if (monitoredFilter is {Value: "false"})
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == false || v.Series.Monitored == false);
            }
            else
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Series.Monitored == true);
            }

            var resource = pagingSpec.ApplyToPage(_episodeService.EpisodesWithoutFiles, v => MapToResource(v, includeSeries, false, includeImages));

            return Ok(resource);
        }
    }
}
