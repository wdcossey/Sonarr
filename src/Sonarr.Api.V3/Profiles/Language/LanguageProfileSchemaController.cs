using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles.Languages;
using Sonarr.Api.V3.Profiles.Language;

namespace NzbDrone.Api.V3.Profiles.Language
{
    [ApiController]
    [Route("/api/v3/languageprofile/schema")]
    public class LanguageProfileSchemaController : ControllerBase//SonarrRestModule<LanguageProfileResource>
    {
        private readonly LanguageProfileService _languageProfileService;

        public LanguageProfileSchemaController(LanguageProfileService languageProfileService)
            //: base("/languageprofile/schema")
        {
            _languageProfileService = languageProfileService;
            /*GetResourceSingle = GetAll;*/
        }

        [HttpGet]
        public LanguageProfileResource GetAll()
        {
            var profile = _languageProfileService.GetDefaultProfile(string.Empty);
            return profile.ToResource();
        }
    }
}
