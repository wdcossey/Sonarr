using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Instrumentation;
using Sonarr.Http;
using Sonarr.Http.Attributes;
using Sonarr.Http.Extensions;
using Sonarr.Http.ModelBinders;

namespace Sonarr.Api.V3.Logs
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ApiController]
    [SonarrApiRoute("log", RouteVersion.V3)]
    public class LogController : ControllerBase
    {
        private readonly ILogService _logService;

        public LogController(ILogService logService)
            => _logService = logService;

        [HttpGet]
        public IActionResult GetLogs(
            [FromQuery] [ModelBinder(typeof(PagingResourceModelBinder))] PagingResource<LogResource> pagingResource)
        {
            var pageSpec = pagingResource.MapToPagingSpec<LogResource, Log>();

            if (pageSpec.SortKey == "time")
                pageSpec.SortKey = "id";

            var levelFilter = pagingResource?.Filters?.FirstOrDefault(f => f.Key == "level");

            if (levelFilter != null)
            {
                switch (levelFilter.Value)
                {
                    case "fatal":
                        pageSpec.FilterExpressions.Add(h => h.Level == "Fatal");
                        break;
                    case "error":
                        pageSpec.FilterExpressions.Add(h => h.Level == "Fatal" || h.Level == "Error");
                        break;
                    case "warn":
                        pageSpec.FilterExpressions.Add(h => h.Level == "Fatal" || h.Level == "Error" || h.Level == "Warn");
                        break;
                    case "info":
                        pageSpec.FilterExpressions.Add(h => h.Level == "Fatal" || h.Level == "Error" || h.Level == "Warn" || h.Level == "Info");
                        break;
                    case "debug":
                        pageSpec.FilterExpressions.Add(h => h.Level == "Fatal" || h.Level == "Error" || h.Level == "Warn" || h.Level == "Info" || h.Level == "Debug");
                        break;
                    case "trace":
                        pageSpec.FilterExpressions.Add(h => h.Level == "Fatal" || h.Level == "Error" || h.Level == "Warn" || h.Level == "Info" || h.Level == "Debug" || h.Level == "Trace");
                        break;
                }
            }

            var response = pageSpec.ApplyToPage(_logService.Paged, LogResourceMapper.ToResource);

            if (pageSpec.SortKey == "id")
                response.SortKey = "time";

            return Ok(response);
        }
    }
}
