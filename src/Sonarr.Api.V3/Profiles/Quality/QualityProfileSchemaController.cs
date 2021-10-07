using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Qualities;
using Sonarr.Api.V3.Profiles.Quality;

namespace NzbDrone.Api.V3.Profiles.Quality
{
    [ApiController]
    [Route("/api/v3/qualityprofile/schema")]
    public class QualityProfileSchemaController : ControllerBase //SonarrRestModule<QualityProfileResource>
    {
        private readonly IQualityProfileService _qualityProfileService;

        public QualityProfileSchemaController(IQualityProfileService qualityProfileService)
            //: base("/qualityprofile/schema")
        {
            _qualityProfileService = qualityProfileService;
            /*GetResourceSingle = GetSchema;*/
        }

        [HttpGet]
        private QualityProfileResource GetSchema()
        {
            var qualityProfile = _qualityProfileService.GetDefaultProfile(string.Empty);

            return qualityProfile.ToResource();
        }
    }
}
