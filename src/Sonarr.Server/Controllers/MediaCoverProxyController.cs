using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NzbDrone.Core.MediaCover;

namespace Sonarr.Server.Controllers
{
    [ApiController]
    [Route("/MediaCoverProxy")]
    public class MediaCoverProxyController : ControllerBase
    {
        private readonly IMediaCoverProxy _mediaCoverProxy;

        public MediaCoverProxyController(IMediaCoverProxy mediaCoverProxy)
            => _mediaCoverProxy = mediaCoverProxy;

        [HttpGet]
        [Route("{hash:required:regex(\\w+)}/{filename:required:regex(((.+))\\.((jpg|png|gif)))}")]
        public IActionResult GetResponse(string hash, string filename)
        {
            var provider = new FileExtensionContentTypeProvider();

            if(!provider.TryGetContentType(filename, out var contentType))
                contentType = MediaTypeNames.Application.Octet;

            var imageData = _mediaCoverProxy.GetImage(hash);

            return new FileContentResult(imageData, contentType);
        }
    }
}
