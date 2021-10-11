using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;

namespace Sonarr.Api.V3.Config
{
    [ApiController]
    [SonarrApiConfigRoute("indexer", RouteVersion.V3)]
    //TODO: Remove `SonarrControllerBase<>`
    public class IndexerConfigController : SonarrConfigController<IndexerConfigResource>
    {
        public IndexerConfigController(IConfigService configService)
            : base(configService)
        {
            /*SharedValidator.RuleFor(c => c.MinimumAge)
                           .GreaterThanOrEqualTo(0);

            SharedValidator.RuleFor(c => c.Retention)
                           .GreaterThanOrEqualTo(0);

            SharedValidator.RuleFor(c => c.RssSyncInterval)
                           .IsValidRssSyncInterval();*/
        }

        protected override IndexerConfigResource ToResource(IConfigService model)
            => IndexerConfigResourceMapper.ToResource(model);
    }
}