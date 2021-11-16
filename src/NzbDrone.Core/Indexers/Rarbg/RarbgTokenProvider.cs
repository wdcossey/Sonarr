using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Indexers.Rarbg
{
    public interface IRarbgTokenProvider
    {
        string GetToken(RarbgSettings settings);
    }

    public class RarbgTokenProvider : IRarbgTokenProvider
    {
        private readonly IHttpClient<RarbgTokenProvider> _httpClient;
        private readonly ICached<string> _tokenCache;

        public RarbgTokenProvider(IHttpClient<RarbgTokenProvider> httpClient, ICacheManager cacheManager)
        {
            _httpClient = httpClient;
            _tokenCache = cacheManager.GetCache<string>(GetType());
        }

        public string GetToken(RarbgSettings settings)
        {
            return _tokenCache.Get(settings.BaseUrl, () =>
                {
                    var requestBuilder = new HttpRequestBuilder(settings.BaseUrl.Trim('/'))
                        .WithRateLimit(3.0)
                        .Resource("/pubapi_v2.php?get_token=get_token&app_id=Sonarr")
                        .Accept(HttpAccept.Json);

                    if (settings.CaptchaToken.IsNotNullOrWhiteSpace())
                    {
                        requestBuilder.UseSimplifiedUserAgent = true;
                        requestBuilder.SetCookie("cf_clearance", settings.CaptchaToken);
                    }

                    var response = _httpClient.Get<JObject>(requestBuilder.Build());

                    return response.Resource["token"].ToString();
                }, TimeSpan.FromMinutes(14.0));
        }
    }
}
