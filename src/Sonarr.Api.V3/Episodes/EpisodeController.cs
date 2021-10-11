using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Tv;
using Sonarr.Http.Extensions;

namespace Sonarr.Api.V3.Episodes
{
    [SonarrApiRoute("episode", RouteVersion.V3)]
    public class EpisodeController : EpisodeControllerBase
    {
        public EpisodeController(
            ISeriesService seriesService,
            IEpisodeService episodeService,
            IUpgradableSpecification upgradableSpecification/*,
            IBroadcastSignalRMessage signalRBroadcaster*/)
            : base(episodeService, seriesService, upgradableSpecification/*, signalRBroadcaster*/) { }

        [HttpGet]
        //[EpisodeIdsFilter]
        public IActionResult GetEpisodes(
            [FromQuery] int? seriesId = null,
            [FromQuery] IList<int> episodeIds = null,
            [FromQuery] int? episodeFileId = null,
            [FromQuery] int? seasonNumber = null,
            [FromQuery] bool includeImages = false)
        {
            if (seriesId.HasValue)
                return Ok(seasonNumber.HasValue
                    ? MapToResource(_episodeService.GetEpisodesBySeason(seriesId.Value, seasonNumber.Value), false, false, includeImages)
                    : MapToResource(_episodeService.GetEpisodeBySeries(seriesId.Value), false, false, includeImages));

            if (episodeIds?.Any() == true)
                return Ok(MapToResource(_episodeService.GetEpisodes(episodeIds), false, false, includeImages));

            if (episodeFileId.HasValue)
                return Ok(MapToResource(_episodeService.GetEpisodesByFileId(episodeFileId.Value), false, false, includeImages));

            return BadRequest($"{nameof(seriesId)} or {nameof(episodeIds)} must be provided");
        }

        [HttpPut("{id:int:required}")]
        public IActionResult SetEpisodeMonitored(int id)
        {
            var resource = Request.Body.FromJson<EpisodeResource>();
            _episodeService.SetEpisodeMonitored(id, resource.Monitored);

            resource = MapToResource(_episodeService.GetEpisode(id), false, false, false);

            return Accepted(resource);
        }

        [HttpPut("monitor")]
        public IActionResult SetEpisodesMonitored([FromBody] EpisodesMonitoredResource resource, [FromQuery] bool includeImages = false)
        {
            if (resource.EpisodeIds.Count == 1)
                _episodeService.SetEpisodeMonitored(resource.EpisodeIds.First(), resource.Monitored);
            else
                _episodeService.SetMonitored(resource.EpisodeIds, resource.Monitored);

            var resources = MapToResource(_episodeService.GetEpisodes(resource.EpisodeIds), false, false, includeImages);

            return Accepted(resources);
        }
    }
}
