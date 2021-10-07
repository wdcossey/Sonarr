using System.Collections.Generic;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NzbDrone.Core.Profiles.Qualities;
using Sonarr.Api.V3.Profiles.Quality;

namespace NzbDrone.Host.WebHost.Quality
{
    public class QualityProfileModule: WebApiController
    {
        private readonly IQualityProfileService _qualityProfileService;

        public QualityProfileModule(IQualityProfileService qualityProfileService)
        {
            _qualityProfileService = qualityProfileService;
        }

        [Route(HttpVerbs.Get, "/")]
        public Task<IList<QualityProfileResource>> GetAllAsync()
        {
            var result = _qualityProfileService.All().ToResource();
            return Task.FromResult<IList<QualityProfileResource>>(result);
        }
    }
}
