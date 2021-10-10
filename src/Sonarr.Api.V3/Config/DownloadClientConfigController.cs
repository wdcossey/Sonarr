using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;

namespace Sonarr.Api.V3.Config
{
    [ApiController]
    [SonarrV3ConfigRoute("downloadclient")]
    public class DownloadClientConfigController : SonarrConfigController<DownloadClientConfigResource>
    {
        public DownloadClientConfigController(IConfigService configService)
            : base(configService) { }

        protected override DownloadClientConfigResource ToResource(IConfigService model)
            => DownloadClientConfigResourceMapper.ToResource(model);
    }
}