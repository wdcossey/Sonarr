using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.DataAugmentation.DailySeries
{
    public interface IDailySeriesDataProxy
    {
        IEnumerable<int> GetDailySeriesIds();
    }

    public class DailySeriesDataProxy : IDailySeriesDataProxy
    {
        private readonly IHttpClient<DailySeriesDataProxy> _httpClient;
        private readonly IHttpRequestBuilderFactory _requestBuilder;
        private readonly ILogger<DailySeriesDataProxy> _logger;

        public DailySeriesDataProxy(IHttpClient<DailySeriesDataProxy> httpClient, ISonarrCloudRequestBuilder requestBuilder, ILogger<DailySeriesDataProxy> logger)
        {
            _httpClient = httpClient;
            _requestBuilder = requestBuilder.Services;
            _logger = logger;
        }

        public IEnumerable<int> GetDailySeriesIds()
        {
            try
            {
                var dailySeriesRequest = _requestBuilder.Create()
                                                        .Resource("/dailyseries")
                                                        .Build();

                var response = _httpClient.Get<List<DailySeries>>(dailySeriesRequest);
                return response.Resource.Select(c => c.TvdbId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get Daily Series");
                return new List<int>();
            }
        }
    }
}