using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace Sonarr.Http.Frontend.Mappers
{
    public abstract class PhysicalFileControllerBase : ControllerBase
    {
        protected ILogger Logger { get; }

        protected PhysicalFileControllerBase(ILogger logger)
        {
            Logger = logger;
        }

        protected IActionResult GetPhysicalFile(string filePath, string fallbackContentType = MediaTypeNames.Application.Octet)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Logger?.LogWarning("File {FilePath} not found", filePath);
                return NotFound();
            }

            var provider = new FileExtensionContentTypeProvider();

            if(!provider.TryGetContentType(filePath, out var contentType))
                contentType = fallbackContentType;

            return new PhysicalFileResult(filePath, contentType);
        }
    }
}
