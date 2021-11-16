using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DiskSpace;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.DiskSpace
{
    [ApiController]
    [SonarrApiRoute("diskspace", RouteVersion.V3)]
    public class DiskSpaceController : ControllerBase
    {
        private readonly IDiskSpaceService _diskSpaceService;

        public DiskSpaceController(IDiskSpaceService diskSpaceService)
            => _diskSpaceService = diskSpaceService;

        [ProducesResponseType(typeof(List<DiskSpaceResource>), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult GetFreeSpace()
            => Ok(_diskSpaceService.GetFreeSpace().ConvertAll(DiskSpaceResourceMapper.MapToResource));
    }
}
