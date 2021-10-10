using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Tags;
using Sonarr.Http;

namespace Sonarr.Api.V3.Tags
{
    [ApiController]
    [SonarrV3Route("tag/detail")]
    public class TagDetailsController : ControllerBase// SonarrRestModule<TagDetailsResource>
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
