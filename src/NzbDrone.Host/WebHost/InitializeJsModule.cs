using System.IO;
using System.Text;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using Nancy;
using Nancy.Responses;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Analytics;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Host.WebHost
{
    public class InitializeJsModule : EmbedIO.WebModuleBase
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IAnalyticsService _analyticsService;

        private static string _apiKey;
        private static string _urlBase;
        private string _generatedContent = null;

        public InitializeJsModule(
            IConfigFileProvider configFileProvider,
            IAnalyticsService analyticsService)
            : base("/initialize.js")
        {
            _configFileProvider = configFileProvider;
            _analyticsService = analyticsService;
        }

        protected override Task OnRequestAsync(IHttpContext context)
        {
            return context.SendStringAsync(GetContent(), "application/javascript", Encoding.UTF8);
        }

        public override bool IsFinalHandler => true;

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
