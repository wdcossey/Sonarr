﻿using System.Collections.Generic;
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

        [HttpGet]
        public List<DiskSpaceResource> GetFreeSpace()
            => _diskSpaceService.GetFreeSpace().ConvertAll(DiskSpaceResourceMapper.MapToResource);
    }
}