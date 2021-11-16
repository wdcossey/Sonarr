﻿using Microsoft.Extensions.Logging;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Download.Clients.DownloadStation.Responses;

namespace NzbDrone.Core.Download.Clients.DownloadStation.Proxies
{
    public interface IDSMInfoProxy
    {
        string GetSerialNumber(DownloadStationSettings settings);
    }

    public class DSMInfoProxy : DiskStationProxyBase, IDSMInfoProxy
    {
        public DSMInfoProxy(IHttpClient<DSMInfoProxy> httpClient, ICacheManager cacheManager, ILogger<DSMInfoProxy> logger) :
            base(DiskStationApi.DSMInfo, "SYNO.DSM.Info", httpClient, cacheManager, logger)
        {
        }

        public string GetSerialNumber(DownloadStationSettings settings)
        {
            var info = GetApiInfo(settings);

            var requestBuilder = BuildRequest(settings, "getinfo", info.MinVersion);

            var response = ProcessRequest<DSMInfoResponse>(requestBuilder, "get serial number", settings);

            return response.Data.SerialNumber;
        }
    }
}
