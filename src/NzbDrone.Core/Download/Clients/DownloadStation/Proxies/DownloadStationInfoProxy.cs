using NzbDrone.Common.Http;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Cache;

namespace NzbDrone.Core.Download.Clients.DownloadStation.Proxies
{
    public interface IDownloadStationInfoProxy : IDiskStationProxy
    {
        Dictionary<string, object> GetConfig(DownloadStationSettings settings);
    }

    public class DownloadStationInfoProxy : DiskStationProxyBase, IDownloadStationInfoProxy
    {
        public DownloadStationInfoProxy(IHttpClient httpClient, ICacheManager cacheManager, ILogger<DownloadStationInfoProxy> logger) :
            base(DiskStationApi.DownloadStationInfo, "SYNO.DownloadStation.Info", httpClient, cacheManager, logger)
        {
        }

        public Dictionary<string, object> GetConfig(DownloadStationSettings settings)
        {
            var requestBuilder = BuildRequest(settings, "getConfig", 1);

            var response = ProcessRequest<Dictionary<string, object>>(requestBuilder, "get config", settings);

            return response.Data;
        }
    }
}
