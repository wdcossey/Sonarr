using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.RootFolders;
using NzbDrone.SignalR;
using Sonarr.Api.V3.RootFolders;
using Sonarr.Http;

namespace NzbDrone.Api.V3.RootFolders
{
    public class RootFolderEventHandler : EventHandlerBase<RootFolderResource, RootFolder>
    {
        private readonly IRootFolderService _rootFolderService;

        public RootFolderEventHandler(IHubContext<SonarrHub, ISonarrHub> hubContext, IRootFolderService rootFolderService) 
            : base(hubContext) => _rootFolderService = rootFolderService;

        protected override RootFolderResource GetResourceById(int id)
            => _rootFolderService.Get(id, true).ToResource();
    }
}
