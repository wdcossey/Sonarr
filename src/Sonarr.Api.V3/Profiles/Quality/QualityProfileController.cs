using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Qualities;
using Sonarr.Api.V3.Profiles.Quality;

namespace NzbDrone.Api.V3.Profiles.Quality
{
    [ApiController]
    [Route("/api/v3/qualityprofile")]
    public class QualityProfileController : ControllerBase //SonarrRestModule<QualityProfileResource>
    {
        private readonly IQualityProfileService _qualityProfileService;

        public QualityProfileController(IQualityProfileService qualityProfileService)
        {
            _qualityProfileService = qualityProfileService;
            /*SharedValidator.RuleFor(c => c.Name).NotEmpty();
            SharedValidator.RuleFor(c => c.Cutoff).ValidCutoff();
            SharedValidator.RuleFor(c => c.Items).ValidItems();

            GetResourceAll = GetAll;
            GetResourceById = GetById;
            UpdateResource = Update;
            CreateResource = Create;
            DeleteResource = DeleteProfile;*/
        }

        [HttpPost]
        public int Create([FromBody] QualityProfileResource resource)
        {
            var model = resource.ToModel();
            model = _qualityProfileService.Add(model);
            return model.Id;
        }

        [HttpDelete("{id:int}")]
        public void DeleteProfile(int id)
        {
            _qualityProfileService.Delete(id);
        }

        [HttpPut]
        public void Update([FromBody] QualityProfileResource resource)
        {
            var model = resource.ToModel();

            _qualityProfileService.Update(model);
        }

        [HttpGet("{id:int}")]
        public QualityProfileResource GetById(int id)
        {
            return _qualityProfileService.Get(id).ToResource();
        }

        [HttpGet]
        public List<QualityProfileResource> GetAll()
        {
            return _qualityProfileService.All().ToResource();
        }
    }
}