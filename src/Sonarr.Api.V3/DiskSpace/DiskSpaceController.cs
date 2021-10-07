using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DiskSpace;

namespace Sonarr.Api.V3.DiskSpace
{
    [ApiController]
    [Route("/api/v3/diskspace")]
    public class DiskSpaceController : ControllerBase// SonarrRestModule<DiskSpaceResource>
    {
    private readonly IDiskSpaceService _diskSpaceService;

    public DiskSpaceController(IDiskSpaceService diskSpaceService)
        //: base("diskspace")
    {
        _diskSpaceService = diskSpaceService;
        //GetResourceAll = GetFreeSpace;
    }

    [HttpGet]
    public List<DiskSpaceResource> GetFreeSpace()
    {
        return _diskSpaceService.GetFreeSpace().ConvertAll(DiskSpaceResourceMapper.MapToResource);
    }
    }
}
