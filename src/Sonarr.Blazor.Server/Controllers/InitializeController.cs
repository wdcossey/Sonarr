using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Analytics;
using NzbDrone.Core.Configuration;

namespace Sonarr.Blazor.Server.Controllers
{
    [Route("[controller]")]
    public class InitializeController : ControllerBase
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IAnalyticsService _analyticsService;

        private static string _apiKey;
        private static string _urlBase;
        private string _generatedContent = null;

        public InitializeController(
            IConfigFileProvider configFileProvider,
            IAnalyticsService analyticsService)
        {
            _configFileProvider = configFileProvider;
            _analyticsService = analyticsService;
        }

        // GET
        [HttpGet("/initialize.js")]
        public Task Index()
        {
            Response.ContentType = "application/javascript";
            return Response.WriteAsync(GetContent(), Encoding.UTF8);
        }

        private string GetContent()
        {
            if (RuntimeInfo.IsProduction && !string.IsNullOrWhiteSpace(_generatedContent))
            {
                return _generatedContent;
            }

            var builder = new StringBuilder();
            builder.AppendLine("window.Sonarr = {");
            builder.AppendLine($"  apiRoot: '{_urlBase}/api/v3',");
            builder.AppendLine($"  apiKey: '{_apiKey}',");
            builder.AppendLine($"  release: '{BuildInfo.Release}',");
            builder.AppendLine($"  version: '{BuildInfo.Version.ToString()}',");
            builder.AppendLine($"  branch: '{_configFileProvider.Branch.ToLower()}',");
            builder.AppendLine($"  analytics: {_analyticsService.IsEnabled.ToString().ToLowerInvariant()},");
            builder.AppendLine($"  urlBase: '{_urlBase}',");
            builder.AppendLine($"  isProduction: {RuntimeInfo.IsProduction.ToString().ToLowerInvariant()}");
            builder.AppendLine("};");

            _generatedContent = builder.ToString();

            return _generatedContent;
        }
    }
}
