using System;
using Microsoft.AspNetCore.Authentication;


namespace Sonarr.Server.Authentication.Extensions
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddSonarrApiKeyScheme(this AuthenticationBuilder authenticationBuilder, Action<ApiKeyAuthenticationOptions>? options = null)
            => authenticationBuilder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.DefaultScheme, options);
    }
}
