using Microsoft.AspNetCore.Builder;
using NzbDrone.SignalR.Middleware;

namespace NzbDrone.SignalR.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSonarrHubApiKeyMiddleware(this IApplicationBuilder app)
            => app.UseMiddleware<SonarrHubApiKeyMiddleware>();
    }
}
