using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Analytics;
using NzbDrone.Core.Configuration;

namespace Sonarr.Http.Frontend
{
    public class InitializeJsController : ControllerBase
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IAnalyticsService _analyticsService;
        private string? _generatedContent;

        public InitializeJsController(
            IConfigFileProvider configFileProvider,
            IAnalyticsService analyticsService)
        {
            _configFileProvider = configFileProvider;
            _analyticsService = analyticsService;
        }

        [HttpGet("initialize.js")]
        public IActionResult Index()
        {
            return new ContentResult
            {
                Content = GetContent(),
                ContentType = "application/javascript",
                StatusCode = StatusCodes.Status200OK
            };
        }

        private string GetContent()
        {
            if (RuntimeInfo.IsProduction && !string.IsNullOrWhiteSpace(_generatedContent))
                return _generatedContent;

            var builder = new StringBuilder();
            builder.AppendLine("window.Sonarr = {");
            builder.AppendLine($"  apiRoot: '{_configFileProvider.UrlBase}/api/v3',");
            builder.AppendLine($"  apiKey: '{_configFileProvider.ApiKey}',");
            builder.AppendLine($"  release: '{BuildInfo.Release}',");
            builder.AppendLine($"  version: '{BuildInfo.Version}',");
            builder.AppendLine($"  branch: '{_configFileProvider.Branch.ToLower()}',");
            builder.AppendLine($"  analytics: {_analyticsService.IsEnabled.ToString().ToLowerInvariant()},");
            builder.AppendLine($"  urlBase: '{_configFileProvider.UrlBase}',");
            builder.AppendLine($"  isProduction: {RuntimeInfo.IsProduction.ToString().ToLowerInvariant()}");
            builder.AppendLine("};");

            return _generatedContent = builder.ToString();
        }
    }
}

