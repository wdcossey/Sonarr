using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Config
{
    [ApiController]
    [SonarrApiConfigRoute("downloadclient", RouteVersion.V3)]
    public class DownloadClientConfigController : SonarrConfigController<DownloadClientConfigResource>
    {
        public DownloadClientConfigController(IConfigService configService)
            : base(configService) { }

        protected override DownloadClientConfigResource ToResource(IConfigService model)
            => DownloadClientConfigResourceMapper.ToResource(model);
    }
}