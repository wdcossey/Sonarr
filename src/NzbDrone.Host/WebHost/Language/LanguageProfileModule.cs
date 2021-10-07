using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using NzbDrone.Core.Profiles.Languages;

namespace NzbDrone.Host.WebHost.Language
{
    public class LanguageProfileModule: EmbedIO.WebApi.WebApiController
    {
        private readonly ILanguageProfileService _profileRepository;

        public LanguageProfileModule(ILanguageProfileService qualityProfileService)
            //: base(qualityProfileService)
        {
            _profileRepository = qualityProfileService;
        }

        [Route(HttpVerbs.Get, "/")]
        public Task<IList<LanguageProfile>> GetAllAsync()
        {
            var result = _profileRepository.All().ToList();
            return Task.FromResult<IList<LanguageProfile>>(result);
        }
    }
}
