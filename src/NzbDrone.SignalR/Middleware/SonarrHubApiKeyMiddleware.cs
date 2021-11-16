using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NzbDrone.SignalR.Middleware
{
    /// <summary>
    /// Gets the ApiKey from the <see cref="HttpRequest.Query"/>, adds it to the <see cref="HttpRequest.Headers"/> for Authentication
    /// </summary>
    internal class SonarrHubApiKeyMiddleware
    {
        private readonly RequestDelegate _next;

        public SonarrHubApiKeyMiddleware(RequestDelegate next)
            => _next = next;

        public Task Invoke(HttpContext httpContext)
        {
            var request = httpContext.Request;

            if (request.Path.StartsWithSegments(SonarrHub.RoutePattern, StringComparison.OrdinalIgnoreCase) && 
                request.Query.TryGetValue("apiKey", out var apiKey))
            {
                request.Headers.Add("x-api-key", apiKey);
            }
             
            return _next(httpContext);
        }
    }
}
