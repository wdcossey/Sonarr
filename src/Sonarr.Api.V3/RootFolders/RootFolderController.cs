using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation.Paths;
using Sonarr.Api.V3.RootFolders;
using Sonarr.Http.Attributes;

namespace NzbDrone.Api.V3.RootFolders
{
    [ApiController]
    [SonarrApiRoute("rootFolder", RouteVersion.V3)]
    public class RootFolderController : ControllerBase
    {
        private readonly IRootFolderService _rootFolderService;

        public RootFolderController(IRootFolderService rootFolderService,
            RootFolderValidator rootFolderValidator,
            PathExistsValidator pathExistsValidator,
            MappedNetworkDriveValidator mappedNetworkDriveValidator,
            StartupFolderValidator startupFolderValidator,
            SystemFolderValidator systemFolderValidator,
            FolderWritableValidator folderWritableValidator)
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

        [HttpGet("{id:int:required}")]
        public IActionResult GetRootFolder(int id, [FromQuery] bool timeout = true)
            => Ok(_rootFolderService.Get(id, timeout).ToResource());

        [HttpPost]
        public IActionResult CreateRootFolder([FromBody] RootFolderResource rootFolderResource)
        {
            var model = _rootFolderService.Add(rootFolderResource.ToModel());
            return Created($"{Request.Path}/{model.Id}", model.ToResource());
        }

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
