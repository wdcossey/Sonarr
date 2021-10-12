using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Languages;
using Sonarr.Api.V3;
using Sonarr.Api.V3.Profiles.Language;

namespace NzbDrone.Api.V3.Profiles.Language
{
    [ApiController]
    [SonarrApiRoute("languageprofile", RouteVersion.V3)]
    public class LanguageProfileController : ControllerBase
    {
        private readonly ILanguageProfileService _profileService;

        public LanguageProfileController(ILanguageProfileService profileService)
        {
            _profileService = profileService;
            /*SharedValidator.RuleFor(c => c.Name).NotEmpty();
            SharedValidator.RuleFor(c => c.Cutoff).NotNull();
            SharedValidator.RuleFor(c => c.Languages).MustHaveAllowedLanguage();*/
        }

        [HttpPost]
        public IActionResult Create([FromBody] LanguageProfileResource resource)
        {
            var model = _profileService.Add(resource.ToModel());
            return Created($"{Request.Path}/{model.Id}", model.ToResource());
        }

        [HttpDelete("{id:int:required}")]
        public IActionResult DeleteProfile(int id)
        {
            _profileService.Delete(id);
            return Ok(new object());
        }

        [HttpPut]
        [HttpPut("{id:int?}")]
        public IActionResult Update(int? id, [FromBody] LanguageProfileResource resource)
        {
            var model = resource.ToModel();
            return Accepted(_profileService.Update(model)?.ToResource());
        }

        [HttpGet("{id:int:required}")]
        public IActionResult GetById(int id)
            => Ok(_profileService.Get(id).ToResource());

        [HttpGet]
        public IActionResult GetAll()
            => Ok(_profileService.All().ToResource());
    }
}