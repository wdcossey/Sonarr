using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Languages;
using Sonarr.Api.V3.Profiles.Language;

namespace NzbDrone.Api.V3.Profiles.Language
{
    [ApiController]
    [Route("/api/v3/languageprofile")]
    public class LanguageProfileController : ControllerBase//SonarrRestModule<LanguageProfileResource>
    {
        private readonly ILanguageProfileService _profileService;

        public LanguageProfileController(ILanguageProfileService profileService)
        {
            _profileService = profileService;
            /*SharedValidator.RuleFor(c => c.Name).NotEmpty();
            SharedValidator.RuleFor(c => c.Cutoff).NotNull();
            SharedValidator.RuleFor(c => c.Languages).MustHaveAllowedLanguage();

            GetResourceAll = GetAll;
            GetResourceById = GetById;
            UpdateResource = Update;
            CreateResource = Create;
            DeleteResource = DeleteProfile;*/
        }

        [HttpPost]
        public int Create([FromBody] LanguageProfileResource resource)
        {
            var model = resource.ToModel();
            model = _profileService.Add(model);
            return model.Id;
        }

        [HttpDelete("{id:int}")]
        public void DeleteProfile(int id)
        {
            _profileService.Delete(id);
        }

        [HttpPut]
        public void Update([FromBody] LanguageProfileResource resource)
        {
            var model = resource.ToModel();

            _profileService.Update(model);
        }

        [HttpGet("{id:int}")]
        public LanguageProfileResource GetById(int id)
        {
            return _profileService.Get(id).ToResource();
        }

        [HttpGet]
        public List<LanguageProfileResource> GetAll()
        {
            var profiles = _profileService.All().ToResource();

            return profiles;
        }
    }
}