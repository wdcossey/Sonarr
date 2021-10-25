using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.EnvironmentInfo;

namespace Sonarr.Http.Frontend
{
    [AllowAnonymous]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class ServiceWorkerController : ControllerBase
    {
        private static string _generatedContent = null;

        [HttpGet("service-worker.js")]
        public IActionResult GetServiceWorker()
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

            //This doesn't need to do anything special, just has to be present for PWA installation
            var builder = new StringBuilder();
            builder.AppendLine("self.addEventListener(\"fetch\", function(event) {");
            builder.AppendLine("  /*console.log(`start server worker`)*/");
            builder.AppendLine("});");

            return _generatedContent = builder.ToString();
        }
    }
}
