using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Qualities;
using Sonarr.Api.V3;
using Sonarr.Api.V3.Profiles.Quality;
using Sonarr.Http.Attributes;

namespace NzbDrone.Api.V3.Profiles.Quality
{
    [ApiController]
    [SonarrApiRoute("qualityprofile", RouteVersion.V3)]
    public class QualityProfileController : ControllerBase
    {
        private readonly IQualityProfileService _qualityProfileService;

        public QualityProfileController(IQualityProfileService qualityProfileService)
        {
            _qualityProfileService = qualityProfileService;
            /*SharedValidator.RuleFor(c => c.Name).NotEmpty();
            SharedValidator.RuleFor(c => c.Cutoff).ValidCutoff();
            SharedValidator.RuleFor(c => c.Items).ValidItems();*/
        }

        [HttpPost]
        [HttpPost("{id:int?}")]
        public IActionResult Create(int? id, [FromBody] QualityProfileResource resource)
        {
            var model = _qualityProfileService.Add(resource.ToModel());
            return Created($"{Request.Path}/{model.Id}", model.ToResource());
        }

        [HttpDelete("{id:int:required}")]
        public IActionResult DeleteProfile(int id)
        {
            _qualityProfileService.Delete(id);
            return Ok(new object());
        }

        [HttpPut]
        public IActionResult Update([FromBody] QualityProfileResource resource)
            => Accepted(_qualityProfileService.Update(resource.ToModel()).ToResource());

        [HttpGet("{id:int:required}")]
        public IActionResult GetById(int id)
            => Ok(_qualityProfileService.Get(id).ToResource());

        [HttpGet]
        public IActionResult GetAll()
            => Ok(_qualityProfileService.All().ToResource());
    }
}