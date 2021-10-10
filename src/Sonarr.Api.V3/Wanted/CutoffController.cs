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
    [SonarrV3Route("wanted/cutoff")]
    public class CutoffController : EpisodeControllerBase
    {
        private readonly IEpisodeCutoffService _episodeCutoffService;

        public CutoffController(IEpisodeCutoffService episodeCutoffService,
                            IEpisodeService episodeService,
                            ISeriesService seriesService,
                            IUpgradableSpecification upgradableSpecification/*,
                            IBroadcastSignalRMessage signalRBroadcaster*/)
            : base(episodeService, seriesService, upgradableSpecification/*, signalRBroadcaster, "wanted/cutoff"*/)
        {
            _episodeCutoffService = episodeCutoffService;
            //GetResourcePaged = GetCutoffUnmetEpisodes;
        }

        [HttpGet]
        [PagingResourceFilter]
        public IActionResult GetCutoffUnmetEpisodes(
            [FromQuery] PagingResource<EpisodeResource> pagingResource,
            [FromQuery] bool includeSeries = false,
            [FromQuery] bool includeEpisodeFile = false,
            [FromQuery] bool includeImages = false)
        {
            var pagingSpec = new PagingSpec<Episode>
            {
                Page = pagingResource.Page,
                PageSize = pagingResource.PageSize,
                SortKey = pagingResource.SortKey,
                SortDirection = pagingResource.SortDirection
            };

            //var includeSeries = true;//Request.GetBooleanQueryParameter("includeSeries");
            //var includeEpisodeFile = true;//Request.GetBooleanQueryParameter("includeEpisodeFile");
            //var includeImages = true;//Request.GetBooleanQueryParameter("includeImages");
            var filter = pagingResource.Filters.FirstOrDefault(f => f.Key == "monitored");

            if (filter != null && filter.Value == "false")
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == false || v.Series.Monitored == false);
            }
            else
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Series.Monitored == true);
            }

            var resource = ApplyToPage(_episodeCutoffService.EpisodesWhereCutoffUnmet, pagingSpec, v => MapToResource(v, includeSeries, includeEpisodeFile, includeImages));

            return Ok(resource);
        }
    }
}
