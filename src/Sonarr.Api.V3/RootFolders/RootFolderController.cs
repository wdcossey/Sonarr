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

            /*
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
        public IActionResult GetRootFolder(int id, [FromQuery] bool timeout = true)
            => Ok(_rootFolderService.Get(id, timeout).ToResource());

        [HttpPost]
        public IActionResult CreateRootFolder([FromBody] RootFolderResource rootFolderResource)
            => Created(Request.Path, _rootFolderService.Add(rootFolderResource.ToModel()));

        [HttpGet]
        public IActionResult GetRootFolders()
            => Ok(_rootFolderService.AllWithUnmappedFolders().ToResource());

        [HttpDelete("{id:int}")]
        public IActionResult DeleteFolder(int id)
        {
            _rootFolderService.Remove(id);
            return Ok(new object());
        }
    }
}
