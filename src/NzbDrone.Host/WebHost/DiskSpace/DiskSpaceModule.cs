using System.Collections.Generic;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NzbDrone.Api.DiskSpace;
using NzbDrone.Core.DiskSpace;

namespace NzbDrone.Host.WebHost.DiskSpace
{
    public class DiskSpaceModule: WebApiController
    {
        private readonly IDiskSpaceService _diskSpaceService;

        public DiskSpaceModule(IDiskSpaceService diskSpaceService)
        {
            _diskSpaceService = diskSpaceService;
        }

        [Route(HttpVerbs.Get, "/")]
        public Task<IList<DiskSpaceResource>> GetFreeSpaceAsync()
        {
            var result = _diskSpaceService.GetFreeSpace().ConvertAll(DiskSpaceResourceMapper.MapToResource);
            return Task.FromResult<IList<DiskSpaceResource>>(result);
        }

    }
}
