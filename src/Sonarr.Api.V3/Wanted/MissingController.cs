using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Tv;
using NzbDrone.SignalR;
using Sonarr.Api.V3.Episodes;
using Sonarr.Http;
using Sonarr.Http.Extensions;

namespace Sonarr.Api.V3.Wanted
{
    [ApiController]
    [SonarrV3Route("wanted/missing")]
    public class MissingController : EpisodeControllerBase
    {
        public MissingController(IEpisodeService episodeService,
                             ISeriesService seriesService,
                             IUpgradableSpecification upgradableSpecification/*,
                             IBroadcastSignalRMessage signalRBroadcaster*/)
            //: base(episodeService, seriesService, upgradableSpecification, signalRBroadcaster, )
            : base(episodeService, seriesService, upgradableSpecification)
        {
            //GetResourcePaged = GetMissingEpisodes;
        }

        [HttpGet]
        [PagingResourceFilter]
        public IActionResult GetMissingEpisodes(
            [FromQuery] PagingResource<EpisodeResource> pagingResource,
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

            if (monitoredFilter != null && monitoredFilter.Value == "false")
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == false || v.Series.Monitored == false);
            }
            else
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Series.Monitored == true);
            }

            var resource = ApplyToPage(_episodeService.EpisodesWithoutFiles, pagingSpec, v => MapToResource(v, includeSeries, false, includeImages));

            return Ok(resource);
        }
    }
}
