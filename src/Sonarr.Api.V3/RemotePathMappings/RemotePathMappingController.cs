using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Validation.Paths;
using Sonarr.Api.V3.RemotePathMappings;

namespace NzbDrone.Api.V3.RemotePathMappings
{
    [ApiController]
    [Route("/api/v3/remotepathmapping")]
    public class RemotePathMappingController : ControllerBase//SonarrRestModule<RemotePathMappingResource>
    {
        private readonly IRemotePathMappingService _remotePathMappingService;

        public RemotePathMappingController(
            IRemotePathMappingService remotePathMappingService,
            PathExistsValidator pathExistsValidator,
            MappedNetworkDriveValidator mappedNetworkDriveValidator)
        {
            _remotePathMappingService = remotePathMappingService;

            /*GetResourceAll = GetMappings;
            GetResourceById = GetMappingById;
            CreateResource = CreateMapping;
            DeleteResource = DeleteMapping;
            UpdateResource = UpdateMapping;

            SharedValidator.RuleFor(c => c.Host)
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
        public RemotePathMappingResource GetMappingById(int id)
            => _remotePathMappingService.Get(id).ToResource();

        [HttpPost]
        public IActionResult CreateMapping([FromBody] RemotePathMappingResource resource)
        {
            var model = resource.ToModel();
            return Ok(_remotePathMappingService.Add(model).Id);
        }

        [HttpGet]
        public IActionResult GetMappings()
            => Ok(_remotePathMappingService.All().ToResource());

        [HttpDelete("{id:int}")]
        public void DeleteMapping(int id)
            => _remotePathMappingService.Remove(id);

        [HttpPut]
        public void UpdateMapping([FromBody] RemotePathMappingResource resource)
        {
            var mapping = resource.ToModel();
            _remotePathMappingService.Update(mapping);
        }
    }
}