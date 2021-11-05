using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Blocklisting;
using Sonarr.Api.V3.Blocklist;
using Sonarr.Http.Attributes;

namespace NzbDrone.Api.V3.Blacklist
{
    [ApiController]
    [SonarrApiRoute("blacklist", RouteVersion.V3)]
    public class BlacklistController : BlocklistController
    {
        public BlacklistController(BlocklistService blocklistService) 
            : base(blocklistService) { }
    }
}
