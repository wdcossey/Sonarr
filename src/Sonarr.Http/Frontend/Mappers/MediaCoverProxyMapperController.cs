using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NzbDrone.Core.MediaCover;

namespace Sonarr.Http.Frontend.Mappers
{
    [Route("MediaCoverProxy")]
    public class MediaCoverProxyMapperController : ControllerBase
    {
        private readonly IMediaCoverProxy _mediaCoverProxy;

        public MediaCoverProxyMapperController(IMediaCoverProxy mediaCoverProxy)
            => _mediaCoverProxy = mediaCoverProxy;

        [HttpGet("{hash:required:regex(\\w+)}/{filename:required:regex(((.+))\\.((jpg|png|gif)))}")]
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
