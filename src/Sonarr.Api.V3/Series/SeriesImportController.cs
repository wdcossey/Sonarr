using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Nancy;
using NzbDrone.Core.Tv;
using Sonarr.Http;
using Sonarr.Http.Extensions;

namespace Sonarr.Api.V3.Series
{
    [ApiController]
    [SonarrApiRoute("series/import", RouteVersion.V3)]
    public class SeriesImportController : ControllerBase //SonarrRestModule<SeriesResource>
    {
        private readonly IAddSeriesService _addSeriesService;

        public SeriesImportController(IAddSeriesService addSeriesService)
            //: base("/series/import")
        {
            _addSeriesService = addSeriesService;
            //Post("/",  x => Import());
        }

        [HttpPost]
        public IActionResult Import([FromBody] List<SeriesResource> resource)
        {
            var newSeries = resource.ToModel();
            return Ok(_addSeriesService.AddSeries(newSeries).ToResource());
        }
    }
}
