using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NzbDrone.Core.Configuration;

namespace Sonarr.Server.Middleware
{
    public class ApiKeyAuthorizationMiddleware
    {
        private const string HEADER_API_KEY_NAME = "X-Api-Key";
        private const string QUERY_API_KEY_NAME = "ApiKey";

        private readonly RequestDelegate _next;

        public ApiKeyAuthorizationMiddleware(RequestDelegate next)
            => _next = next;

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api/", StringComparison.InvariantCultureIgnoreCase))
            {
                var apiKey = GetApiKey(context.Request);

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Api Key was not provided");
                    return;
                }

                var configFileProvider = context.RequestServices.GetRequiredService<IConfigFileProvider>();

                if (configFileProvider.ApiKey?.Equals(apiKey) != true)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Api Key is not valid");
                    return;
                }
            }

            await _next(context);
        }

        private string GetApiKey(HttpRequest request)
        {
            request.Headers.TryGetValue(HEADER_API_KEY_NAME, out var apiKeyHeader);

            if (!string.IsNullOrWhiteSpace(apiKeyHeader))
                return apiKeyHeader;

            request.Query.TryGetValue(QUERY_API_KEY_NAME, out var apiKeyQueryString);

            if (!string.IsNullOrWhiteSpace(apiKeyQueryString))
                return apiKeyQueryString;

            return null;//context.Request.Headers.Authorization;
        }
    }
}
