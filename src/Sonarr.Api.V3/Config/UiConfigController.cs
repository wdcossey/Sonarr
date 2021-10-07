using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;

namespace Sonarr.Api.V3.Config
{
    [ApiController]
    [Route("/api/v3/config/ui")]
    public class UiConfigController : SonarrConfigController<UiConfigResource>
    {
        public UiConfigController(IConfigService configService)
            : base(configService) { }

        protected override UiConfigResource ToResource(IConfigService model)
            => UiConfigResourceMapper.ToResource(model);
    }
}