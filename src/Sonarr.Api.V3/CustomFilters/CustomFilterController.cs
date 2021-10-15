using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.CustomFilters;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.CustomFilters
{
    [ApiController]
    [SonarrApiRoute("customFilter", RouteVersion.V3)]
    public class CustomFilterController : ControllerBase
    {
        private readonly ICustomFilterService _customFilterService;

        public CustomFilterController(ICustomFilterService customFilterService)
            => _customFilterService = customFilterService;

        [HttpGet("{id:int:required}")]
        public IActionResult GetCustomFilter(int id)
            => Ok(_customFilterService.Get(id).ToResource());

        [HttpGet]
        public IActionResult GetCustomFilters()
            => Ok(_customFilterService.All().ToResource());

        [HttpPost]
        public IActionResult AddCustomFilter([FromBody] CustomFilterResource resource)
        {
            var model = _customFilterService.Add(resource.ToModel());
            return Created($"{Request.Path}/{model.Id}", model.ToResource());
        }

        [HttpPut]
        [HttpPut("{id:int?}")]
        public IActionResult UpdateCustomFilter(int? id, [FromBody] CustomFilterResource resource)
        {
            var model = _customFilterService.Update(resource.ToModel());
            return Accepted(model.ToResource());
        }

        [HttpDelete("{id:int:required}")]
        public IActionResult DeleteCustomResource(int id)
        {
            _customFilterService.Delete(id);
            return Ok(new object());
        }
    }
}
