using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Common.TPL;

namespace NzbDrone.Common.Http
{
    public interface IHttpClient
    {
        HttpResponse Execute(HttpRequest request);
        void DownloadFile(string url, string fileName);
        HttpResponse Get(HttpRequest request);
        HttpResponse<T> Get<T>(HttpRequest request) where T : new();
        HttpResponse Head(HttpRequest request);
        HttpResponse Post(HttpRequest request);
        HttpResponse<T> Post<T>(HttpRequest request) where T : new();
    }
    
    public interface IHttpClient<TService> : IHttpClient { }

    public class HttpClient<TService> : IHttpClient<TService>
    {
        private const int MaxRedirects = 5;

        private readonly ILogger<HttpClient> _logger;
        private readonly IRateLimitService _rateLimitService;
        private readonly ICached<CookieContainer> _cookieContainerCache;
        private readonly List<IHttpRequestInterceptor> _requestInterceptors;
        private readonly IHttpDispatcher _httpDispatcher;
        private readonly IUserAgentBuilder _userAgentBuilder;

        public HttpClient(IEnumerable<IHttpRequestInterceptor> requestInterceptors,
            ICacheManager cacheManager,
            IRateLimitService rateLimitService,
            IHttpDispatcher httpDispatcher,
            IUserAgentBuilder userAgentBuilder,
            ILogger<HttpClient> logger)
        {
            _requestInterceptors = requestInterceptors.ToList();
            _rateLimitService = rateLimitService;
            _httpDispatcher = httpDispatcher;
            _userAgentBuilder = userAgentBuilder;
            _logger = logger;
            
            ServicePointManager.DefaultConnectionLimit = 12;
            _cookieContainerCache = cacheManager.GetCache<CookieContainer>(typeof(HttpClient<TService>));
        }

        public HttpResponse Execute(HttpRequest request)
        {
            var cookieContainer = InitializeRequestCookies(request);

            var response = ExecuteRequest(request, cookieContainer);

            if (request.AllowAutoRedirect && response.HasHttpRedirect)
            {
                var autoRedirectChain = new List<string>();
                autoRedirectChain.Add(request.Url.ToString());

                do
                {
                    request.Url += new HttpUri(response.Headers.GetSingleValue("Location"));
                    autoRedirectChain.Add(request.Url.ToString());

                    _logger.LogTrace("Redirected to {Url}", request.Url);

                    if (autoRedirectChain.Count > MaxRedirects)
                    {
                        throw new WebException($"Too many automatic redirections were attempted for {autoRedirectChain.Join(" -> ")}", WebExceptionStatus.ProtocolError);
                    }

                    response = ExecuteRequest(request, cookieContainer);
                }
                while (response.HasHttpRedirect);
            }

            if (response.HasHttpRedirect && !RuntimeInfo.IsProduction)
            {
                _logger.LogError("Server requested a redirect to [{LocationHeader}] while in developer mode. Update the request URL to avoid this redirect.", response.Headers["Location"]);
            }

            if (!request.SuppressHttpError && response.HasHttpError && (request.SuppressHttpErrorStatusCodes == null || !request.SuppressHttpErrorStatusCodes.Contains(response.StatusCode)))
            {
                if (request.LogHttpError)
                {
                    _logger.LogWarning("HTTP Error - {Response}", response);
                }

                if ((int)response.StatusCode == 429)
                {
                    throw new TooManyRequestsException(request, response);
                }
                else
                {
                    throw new HttpException(request, response);
                }
            }

            return response;
        }

        private HttpResponse ExecuteRequest(HttpRequest request, CookieContainer cookieContainer)
        {
            foreach (var interceptor in _requestInterceptors)
            {
                request = interceptor.PreRequest(request);
            }

            if (request.RateLimit != TimeSpan.Zero)
            {
                _rateLimitService.WaitAndPulse(request.Url.Host, request.RateLimitKey, request.RateLimit);
            }

            _logger.LogTrace("{Request}", request);

            var stopWatch = Stopwatch.StartNew();

            PrepareRequestCookies(request, cookieContainer);

            var response = _httpDispatcher.GetResponse(request, cookieContainer);

            HandleResponseCookies(response, cookieContainer);

            stopWatch.Stop();

            _logger.LogTrace("{Response} ({ElapsedMilliseconds} ms)", response, stopWatch.ElapsedMilliseconds);

            foreach (var interceptor in _requestInterceptors)
            {
                response = interceptor.PostResponse(response);
            }

            if (request.LogResponseContent && response.ResponseData != null)
            {
                _logger.LogTrace("Response content ({Length} bytes): {Content}", response.ResponseData.Length, response.Content);
            }

            return response;
        }

        private CookieContainer InitializeRequestCookies(HttpRequest request)
        {
            lock (_cookieContainerCache)
            {
                var sourceContainer = new CookieContainer();

                var presistentContainer = _cookieContainerCache.Get("container", () => new CookieContainer());
                var persistentCookies = presistentContainer.GetCookies((Uri)request.Url);
                sourceContainer.Add(persistentCookies);

                if (request.Cookies.Count != 0)
                {
                    foreach (var pair in request.Cookies)
                    {
                        Cookie cookie;
                        if (pair.Value == null)
                        {
                            cookie = new Cookie(pair.Key, "", "/")
                            {
                                Expires = DateTime.Now.AddDays(-1)
                            };
                        }
                        else
                        {
                            cookie = new Cookie(pair.Key, pair.Value, "/")
                            {
                                // Use Now rather than UtcNow to work around Mono cookie expiry bug.
                                // See https://gist.github.com/ta264/7822b1424f72e5b4c961
                                Expires = DateTime.Now.AddHours(1)
                            };
                        }

                        sourceContainer.Add((Uri)request.Url, cookie);

                        if (request.StoreRequestCookie)
                        {
                            presistentContainer.Add((Uri)request.Url, cookie);
                        }
                    }
                }

                return sourceContainer;
            }
        }

        private void PrepareRequestCookies(HttpRequest request, CookieContainer cookieContainer)
        {
            // Don't collect persistnet cookies for intermediate/redirected urls.
            /*lock (_cookieContainerCache)
            {
                var presistentContainer = _cookieContainerCache.Get("container", () => new CookieContainer());
                var persistentCookies = presistentContainer.GetCookies((Uri)request.Url);
                var existingCookies = cookieContainer.GetCookies((Uri)request.Url);

                cookieContainer.Add(persistentCookies);
                cookieContainer.Add(existingCookies);
            }*/
        }

        private void HandleResponseCookies(HttpResponse response, CookieContainer cookieContainer)
        {
            var cookieHeaders = response.GetCookieHeaders();
            if (cookieHeaders.Empty())
            {
                return;
            }

            if (response.Request.StoreResponseCookie)
            {
                lock (_cookieContainerCache)
                {
                    var persistentCookieContainer = _cookieContainerCache.Get("container", () => new CookieContainer());

                    foreach (var cookieHeader in cookieHeaders)
                    {
                        try
                        {
                            persistentCookieContainer.SetCookies((Uri)response.Request.Url, cookieHeader);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Invalid cookie in {Url}", response.Request.Url);
                        }
                    }
                }
            }
        }

        public void DownloadFile(string url, string fileName)
        {
            var fileNamePart = fileName + ".part";

            try
            {
                var fileInfo = new FileInfo(fileName);
                if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }

                _logger.LogDebug("Downloading [{Url}] to [{FileName}]", url, fileName);

                var stopWatch = Stopwatch.StartNew();
                using (var fileStream = new FileStream(fileNamePart, FileMode.Create, FileAccess.ReadWrite))
                {
                    var request = new HttpRequest(url);
                    request.ResponseStream = fileStream;
                    var response = Get(request);

                    if (response.Headers.ContentType != null && response.Headers.ContentType.Contains("text/html"))
                    {
                        throw new HttpException(request, response, "Site responded with html content.");
                    }
                }
                stopWatch.Stop();
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                File.Move(fileNamePart, fileName);
                _logger.LogDebug("Downloading Completed. took {Seconds:0}s", stopWatch.Elapsed.Seconds);
            }
            finally
            {
                if (File.Exists(fileNamePart))
                {
                    File.Delete(fileNamePart);
                }
            }
        }

        public HttpResponse Get(HttpRequest request)
        {
            request.Method = HttpMethod.GET;
            return Execute(request);
        }

        public HttpResponse<T> Get<T>(HttpRequest request) where T : new()
        {
            var response = Get(request);
            CheckResponseContentType(response);
            return new HttpResponse<T>(response);
        }

        public HttpResponse Head(HttpRequest request)
        {
            request.Method = HttpMethod.HEAD;
            return Execute(request);
        }

        public HttpResponse Post(HttpRequest request)
        {
            request.Method = HttpMethod.POST;
            return Execute(request);
        }

        public HttpResponse<T> Post<T>(HttpRequest request) where T : new()
        {
            var response = Post(request);
            CheckResponseContentType(response);
            return new HttpResponse<T>(response);
        }

        private void CheckResponseContentType(HttpResponse response)
        {
            if (response.Headers.ContentType != null && response.Headers.ContentType.Contains("text/html"))
            {
                throw new UnexpectedHtmlContentException(response);
            }
        }
    }
}
