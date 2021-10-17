using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Tv;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Series
{
    [ApiController]
    [SonarrApiRoute("series/import", RouteVersion.V3)]
    public class SeriesImportController : ControllerBase
    {
        private readonly IAddSeriesService _addSeriesService;

        public SeriesImportController(IAddSeriesService addSeriesService)
            => _addSeriesService = addSeriesService;

        [HttpPost]
        public IActionResult Import([FromBody] List<SeriesResource> resource)
        {
            var newSeries = resource.ToModel();
            return Ok(_addSeriesService.AddSeries(newSeries).ToResource());
        }
    }
}
