using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NzbDrone.Common.EnvironmentInfo;

namespace Sonarr.Server.Middleware
{
    /// <summary>
    /// Adds the Server & Version to every response Header
    /// </summary>
    public class SonarrResponseHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public SonarrResponseHeaderMiddleware(RequestDelegate next)
            => _next = next;

        public Task Invoke(HttpContext context)
        {
            context.Response.OnStarting(state =>
            {
                var httpContext = state as HttpContext;
                httpContext?.Response.Headers.TryAdd("X-Application-Version", BuildInfo.Version.ToString());
                httpContext?.Response.Headers.TryAdd("Server", BuildInfo.AppName);
                return Task.CompletedTask;
            }, context);

            return _next(context);
        }
    }
}
