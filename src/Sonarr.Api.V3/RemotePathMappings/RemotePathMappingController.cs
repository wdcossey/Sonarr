using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Validation.Paths;
using Sonarr.Api.V3;
using Sonarr.Api.V3.RemotePathMappings;

namespace NzbDrone.Api.V3.RemotePathMappings
{
    [ApiController]
    [SonarrApiRoute("remotepathmapping", RouteVersion.V3)]
    public class RemotePathMappingController : ControllerBase//SonarrRestModule<RemotePathMappingResource>
    {
        private readonly IRemotePathMappingService _remotePathMappingService;

        public RemotePathMappingController(
            IRemotePathMappingService remotePathMappingService,
            PathExistsValidator pathExistsValidator,
            MappedNetworkDriveValidator mappedNetworkDriveValidator)
        {
            _remotePathMappingService = remotePathMappingService;

            /*SharedValidator.RuleFor(c => c.Host)
                             .NotEmpty();

            // We cannot use IsValidPath here, because it's a remote path, possibly other OS.
            SharedValidator.RuleFor(c => c.RemotePath)
                           .NotEmpty();

            SharedValidator.RuleFor(c => c.LocalPath)
                           .Cascade(CascadeMode.StopOnFirstFailure)
                           .IsValidPath()
                           .SetValidator(mappedNetworkDriveValidator)
                           .SetValidator(pathExistsValidator);*/
        }

        [HttpGet("{id:int}")]
        public IActionResult GetMappingById(int id)
            => Ok(_remotePathMappingService.Get(id).ToResource());

        [HttpPost]
        public IActionResult CreateMapping([FromBody] RemotePathMappingResource resource)
        {
            var remotePathMapping = _remotePathMappingService.Add(resource.ToModel());
            return Created($"{Request.Path}/{remotePathMapping.Id}", remotePathMapping.ToResource());
        }

        [HttpGet]
        public IActionResult GetMappings()
            => Ok(_remotePathMappingService.All().ToResource());

        [HttpDelete("{id:int}")]
        public IActionResult DeleteMapping(int id)
        {
            _remotePathMappingService.Remove(id);
            return Ok(new object());
        }

        [HttpPut]
        [HttpPut("{id:int?}")]
        public IActionResult UpdateMapping(int? id, [FromBody] RemotePathMappingResource resource)
        {
            var mapping = _remotePathMappingService.Update(resource.ToModel());
            return Accepted(mapping.ToResource());
        }
    }
}