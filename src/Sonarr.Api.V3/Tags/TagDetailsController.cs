using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Tags;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Tags
{
    [ApiController]
    [SonarrApiRoute("tag/detail", RouteVersion.V3)]
    public class TagDetailsController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagDetailsController(ITagService tagService)
            => _tagService = tagService;

        [HttpGet("{id:int:required}")]
        public IActionResult GetTagDetails(int id)
            => Ok(_tagService.Details(id).ToResource());

        [HttpGet]
        public IActionResult GetAll()
            => Ok(_tagService.Details().ToResource());

    }
}
