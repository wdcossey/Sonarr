using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Config
{
    [ApiController]
    [SonarrApiConfigRoute("ui", RouteVersion.V3)]
    public class UiConfigController : SonarrConfigController<UiConfigResource>
    {
        public UiConfigController(IConfigService configService)
            : base(configService) { }

        protected override UiConfigResource ToResource(IConfigService model)
            => UiConfigResourceMapper.ToResource(model);
    }
}