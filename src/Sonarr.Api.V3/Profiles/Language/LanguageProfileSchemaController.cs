using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Languages;
using Sonarr.Api.V3;
using Sonarr.Api.V3.Profiles.Language;
using Sonarr.Http.Attributes;

namespace NzbDrone.Api.V3.Profiles.Language
{
    [ApiController]
    [SonarrApiRoute("languageprofile/schema", RouteVersion.V3)]
    public class LanguageProfileSchemaController : ControllerBase
    {
        private readonly LanguageProfileService _languageProfileService;

        public LanguageProfileSchemaController(LanguageProfileService languageProfileService)
            => _languageProfileService = languageProfileService;

        [HttpGet]
        public IActionResult GetAll()
            => Ok(_languageProfileService.GetDefaultProfile(string.Empty).ToResource());
    }
}
