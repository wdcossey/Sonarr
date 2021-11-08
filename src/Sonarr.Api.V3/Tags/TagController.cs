using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Tags;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Tags
{
    [ApiController]
    [SonarrApiRoute("tag", RouteVersion.V3)]
    public class TagController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
            => _tagService = tagService;

        [HttpGet("{id:int:required}")]
        public IActionResult GetTag(int id)
            => Ok(_tagService.GetTag(id).ToResource());

        [HttpGet]
        public IActionResult GetAll()
            => Ok(_tagService.All().ToResource());

        [HttpPost]
        public IActionResult Create([FromBody] TagResource resource)
        {
            var model = _tagService.Add(resource.ToModel());
            return Created($"{Request.Path}/{model.Id}", model.ToResource());
        }

        [HttpPut]
        public IActionResult Update([FromBody] TagResource resource)
            => Accepted(_tagService.Update(resource.ToModel()).ToResource());


        [HttpDelete("{id:int:required}")]
        public IActionResult DeleteTag(int id)
        {
            _tagService.Delete(id);
            return Ok(new object());
        }
    }
}
