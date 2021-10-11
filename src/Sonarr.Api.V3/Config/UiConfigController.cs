using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using NotImplementedException = System.NotImplementedException;

namespace Sonarr.Api.V3.Config
{
    [ApiController]
    [SonarrApiConfigRoute("ui", RouteVersion.V3)]
    //TODO: Remove `SonarrControllerBase<>`
    public class UiConfigController : SonarrConfigController<UiConfigResource>
    {
        public UiConfigController(IConfigService configService)
            : base(configService) { }

        protected override UiConfigResource ToResource(IConfigService model)
            => UiConfigResourceMapper.ToResource(model);

        protected override Task DeleteResourceByIdAsync(int id)
            => throw new NotImplementedException();

        protected override Task<UiConfigResource> CreateResourceAsync(UiConfigResource resource)
            => throw new NotImplementedException();
    }
}