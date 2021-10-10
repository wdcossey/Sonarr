using System.Linq;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Instrumentation;
using Sonarr.Api.V3.Logs;
using Sonarr.Http;

namespace NzbDrone.Host.WebHost.Logs
{
    public class LogModule: WebApiController
    {
        private readonly ILogService _logService;

        public LogModule(ILogService logService)
        {
            _logService = logService;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task<PagingResource<LogResource>> GetLogsAsync([QueryField] int page, [QueryField] int pageSize, [QueryField] SortDirection sortDirection, [QueryField] string sortKey, [QueryField] string level)
        {
            var pageSpec = new PagingSpec<Log>()
            {
                Page = page,
                PageSize = pageSize,
                SortKey = sortKey,
                SortDirection = sortDirection,
            };// pagingResource.MapToPagingSpec<LogResource, Log>();

            if (pageSpec.SortKey == "time")
            {
                pageSpec.SortKey = "id";
            }

            //var levelFilter = pagingResource.Filters.FirstOrDefault(f => f.Key == "level");
            PagingResourceFilter levelFilter = new() { Key = "level", Value = level};// pagingResource.Filters.FirstOrDefault();

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

            var pagingSpec = _logService.Paged(pageSpec);

            var result = new PagingResource<LogResource>
            {
                Page = pagingSpec.Page,
                PageSize = pagingSpec.PageSize,
                SortDirection = pagingSpec.SortDirection,
                SortKey = pagingSpec.SortKey,
                TotalRecords = pagingSpec.TotalRecords,
                Records = pagingSpec.Records.ConvertAll(LogResourceMapper.ToResource)
            };

            if (pageSpec.SortKey == "id")
                result.SortKey = "time";

            return result;

            //return ApplyToPage(_logService.Paged, pageSpec, LogResourceMapper.ToResource);
        }
    }
}
