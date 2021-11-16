using Microsoft.AspNetCore.Authentication;

namespace Sonarr.Server.Authentication
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "api key";
        public static string Scheme => DefaultScheme;
        public string AuthenticationType = DefaultScheme;
    }
}
