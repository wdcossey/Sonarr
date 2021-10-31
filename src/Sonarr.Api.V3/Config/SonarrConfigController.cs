using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using Sonarr.Http.REST;

namespace Sonarr.Api.V3.Config
{
    public abstract class SonarrConfigController<TResource> : ControllerBase
        where TResource : RestResource, new()
    {
        private readonly IConfigService _configService;

        protected SonarrConfigController(IConfigService configService)
        {
            _configService = configService;
            /*
             GetResourceSingle = GetConfig;
             GetResourceById = GetConfig;
             UpdateResource = SaveConfig;
            */
        }

        protected abstract TResource ToResource(IConfigService model);

        [HttpGet]
        public IActionResult GetConfig([FromQuery] int? id = null)
            => Ok(GetConfigResource());

        [HttpPut]
        public IActionResult SaveConfig([FromBody] TResource resource, [FromQuery] int? id = null)
        {
            var dictionary = resource
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(resource, null));

            _configService.SaveConfigDictionary(dictionary);

            return Accepted(GetConfigResource());
        }

        private TResource GetConfigResource()
        {
            var resource = ToResource(_configService);
            resource.Id = 1;
            return resource;
        }
    }
}
