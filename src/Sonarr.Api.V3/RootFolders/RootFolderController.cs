using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation.Paths;
using Sonarr.Api.V3.RootFolders;

namespace NzbDrone.Api.V3.RootFolders
{
    [ApiController]
    [Route("/api/v3/rootFolder")]
    public class RootFolderController : ControllerBase// SonarrRestModuleWithSignalR<RootFolderResource, RootFolder>
    {
        private readonly IRootFolderService _rootFolderService;

        public RootFolderController(IRootFolderService rootFolderService/*,
                                IBroadcastSignalRMessage signalRBroadcaster*/,
                                RootFolderValidator rootFolderValidator,
                                PathExistsValidator pathExistsValidator,
                                MappedNetworkDriveValidator mappedNetworkDriveValidator,
                                StartupFolderValidator startupFolderValidator,
                                SystemFolderValidator systemFolderValidator,
                                FolderWritableValidator folderWritableValidator
            )
            //: base(signalRBroadcaster)
        {
            _rootFolderService = rootFolderService;

            /*GetResourceAll = GetRootFolders;
            GetResourceById = GetRootFolder;
            CreateResource = CreateRootFolder;
            DeleteResource = DeleteFolder;

            SharedValidator.RuleFor(c => c.Path)
                           .Cascade(CascadeMode.StopOnFirstFailure)
                           .IsValidPath()
                           .SetValidator(rootFolderValidator)
                           .SetValidator(mappedNetworkDriveValidator)
                           .SetValidator(startupFolderValidator)
                           .SetValidator(pathExistsValidator)
                           .SetValidator(systemFolderValidator)
                           .SetValidator(folderWritableValidator);*/
        }

        [HttpGet("{id:int}")]
        public RootFolderResource GetRootFolder(int id, [FromQuery] bool? timeout = true)
        {
            //var timeout = Context?.Request?.GetBooleanQueryParameter("timeout", true) ?? true;
            return _rootFolderService.Get(id, timeout ?? true).ToResource();
        }

        [HttpPost]
        public int CreateRootFolder([FromBody] RootFolderResource rootFolderResource)
        {
            var model = rootFolderResource.ToModel();
            return _rootFolderService.Add(model).Id;
        }

        [HttpGet]
        public List<RootFolderResource> GetRootFolders()
            => _rootFolderService.AllWithUnmappedFolders().ToResource();

        [HttpDelete("{id:int}")]
        public void DeleteFolder(int id)
            => _rootFolderService.Remove(id);

    }
}
