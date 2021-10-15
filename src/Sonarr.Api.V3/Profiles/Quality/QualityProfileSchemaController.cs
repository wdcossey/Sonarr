using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Qualities;
using Sonarr.Api.V3;
using Sonarr.Api.V3.Profiles.Quality;
using Sonarr.Http.Attributes;

namespace NzbDrone.Api.V3.Profiles.Quality
{
    [ApiController]
    [SonarrApiRoute("qualityprofile/schema", RouteVersion.V3)]
    public class QualityProfileSchemaController : ControllerBase
    {
        private readonly IQualityProfileService _qualityProfileService;

        public QualityProfileSchemaController(IQualityProfileService qualityProfileService)
            => _qualityProfileService = qualityProfileService;

        [HttpGet]
        public IActionResult GetSchema()
            => Ok(_qualityProfileService.GetDefaultProfile(string.Empty).ToResource());
    }
}