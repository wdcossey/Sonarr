using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Tv;
using Sonarr.Api.V3.Episodes;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Calendar
{
    [ApiController]
    [SonarrApiRoute("calendar", RouteVersion.V3)]
    public class CalendarController : EpisodeControllerBase
    {
        public CalendarController(IEpisodeService episodeService,
                              ISeriesService seriesService,
                              IUpgradableSpecification upgradableSpecification)
            : base(episodeService, seriesService, upgradableSpecification) { }

        [ProducesResponseType(typeof(IEnumerable<EpisodeResource>), StatusCodes.Status201Created)]
        [HttpGet]
        public IActionResult GetCalendar(
            [FromQuery] bool unmonitored = false,
            [FromQuery] DateTime? start = null,
            [FromQuery] DateTime? end = null,
            [FromQuery] bool includeSeries = false,
            [FromQuery] bool includeEpisodeFile = false,
            [FromQuery] bool includeEpisodeImages = false)
        {
            var resources = MapToResource(_episodeService.EpisodesBetweenDates(start ?? DateTime.Today, end ?? DateTime.Today.AddDays(2), unmonitored), includeSeries, includeEpisodeFile, includeEpisodeImages);
            return Ok(resources.OrderBy(e => e.AirDateUtc));
        }
    }
}
