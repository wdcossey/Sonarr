using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NzbDrone.Core.Configuration;

namespace Sonarr.Server.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfigFileProvider _configFileProvider;

    public const string ApiKeyHeaderName = "X-Api-Key";
    
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IConfigFileProvider configFileProvider) : base(options, logger, encoder, clock)
    {
        _configFileProvider = configFileProvider;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

        if (apiKeyHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var existingApiKey = _configFileProvider.ApiKey;

        if (existingApiKey.Equals(providedApiKey, StringComparison.InvariantCulture))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "Sonarr Administrator"),
                new(ClaimTypes.Role, "sonarr-api"),
                new(ClaimTypes.Role, "sonarr-admin"),
                new(ClaimTypes.Role, "sonarr-update"),
                new(ClaimTypes.Role, "sonarr-create"),
                new(ClaimTypes.Role, "sonarr-delete"),
                new(ClaimTypes.Role, "sonarr-signalr"),
                new(ClaimTypes.Role, "sonarr-restart"),
                new(ClaimTypes.Role, "sonarr-shutdown"),
            };

            var identity = new ClaimsIdentity(claims, Options.AuthenticationType);
            var identities = new List<ClaimsIdentity> { identity };
            var principal = new ClaimsPrincipal(identities);
            var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationOptions.Scheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = MediaTypeNames.Application.Json;
        await Response.WriteAsync(JsonSerializer.Serialize(new { Response.StatusCode }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = MediaTypeNames.Application.Json;
        await Response.WriteAsync(JsonSerializer.Serialize(new { Response.StatusCode }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
}
