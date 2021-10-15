using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using NzbDrone.Core.Tv;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.SeasonPass
{
    [ApiController]
    [SonarrApiRoute("seasonpass", RouteVersion.V3)]
    public class SeasonPassController : ControllerBase
    {
        private readonly ISeriesService _seriesService;
        private readonly IEpisodeMonitoredService _episodeMonitoredService;

        public SeasonPassController(
            ISeriesService seriesService,
            IEpisodeMonitoredService episodeMonitoredService)
        {
            _seriesService = seriesService;
            _episodeMonitoredService = episodeMonitoredService;
        }

        [HttpPost]
        public IActionResult UpdateAll([FromBody] SeasonPassResource request)
        {
            //Read from request
            //var request = Request.Body.FromJson<SeasonPassResource>();
            var seriesToUpdate = _seriesService.GetSeries(request.Series.Select(s => s.Id));

            foreach (var s in request.Series)
            {
                var series = seriesToUpdate.Single(c => c.Id == s.Id);

                if (s.Monitored.HasValue)
                {
                    series.Monitored = s.Monitored.Value;
                }

                if (s.Seasons != null && s.Seasons.Any())
                {
                    foreach (var seriesSeason in series.Seasons)
                    {
                        var season = s.Seasons.FirstOrDefault(c => c.SeasonNumber == seriesSeason.SeasonNumber);

                        if (season != null)
                        {
                            seriesSeason.Monitored = season.Monitored;
                        }
                    }
                }

                if (request.MonitoringOptions != null && request.MonitoringOptions.Monitor == MonitorTypes.None)
                {
                    series.Monitored = false;
                }

                _episodeMonitoredService.SetEpisodeMonitoredStatus(series, request.MonitoringOptions);
            }

            return new AcceptedResult
            {
                Value = "ok",
                ContentTypes = new MediaTypeCollection { MediaTypeNames.Application.Json }
            };
        }
    }
}
