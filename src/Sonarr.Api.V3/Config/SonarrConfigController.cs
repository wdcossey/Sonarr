using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using Sonarr.Http.REST;

namespace Sonarr.Api.V3.Config
{
    public abstract class SonarrConfigController<TResource> : SonarrControllerBase<TResource>
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

        /// <summary>
        /// Override base implementation of `GetAllAsync` as we only want a single resource
        /// </summary>
        /// <returns></returns>
        public override Task<IActionResult> GetAllAsync()
            => Task.FromResult<IActionResult>(Ok(GetConfig()));

        /// <summary>
        /// GetAllResourcesAsync is overridden above by `GetAllAsync`
        /// </summary>
        /// <returns></returns>
        protected override Task<IList<TResource>> GetAllResourcesAsync()
            => throw new NotImplementedException();

        protected override Task<TResource> GetResourceByIdAsync(int id)
            => Task.FromResult(GetConfig());

        protected override Task<TResource> UpdateResourceAsync(TResource resource)
            => Task.FromResult(SaveConfig(resource));

        protected override Task<TResource> CreateResourceAsync(TResource resource)
            => throw new NotImplementedException();
            //=> Task.FromResult(SaveConfig(resource));

        protected override Task DeleteResourceByIdAsync(int id)
            => throw new NotImplementedException();

        private TResource GetConfig()
        {
            var resource = ToResource(_configService);
            resource.Id = 1;
            return resource;
        }

        private TResource SaveConfig(TResource resource)
        {
            var dictionary = resource.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(resource, null));

            _configService.SaveConfigDictionary(dictionary);

            return GetConfig();
        }
    }

    public abstract class SonarrConfigControllerOri<TResource> : ControllerBase
            where TResource : RestResource, new()
    {
            private readonly IConfigService _configService;

            protected SonarrConfigControllerOri(IConfigService configService)
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
            public TResource GetConfig()
            {
                var resource = ToResource(_configService);
                resource.Id = 1;

                return resource;
            }

            [HttpGet("{id:int}")]
            public TResource GetConfig(int id)
            {
                return GetConfig();
            }

            [HttpPut]
            public void SaveConfig(TResource resource)
            {
                var dictionary = resource.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .ToDictionary(prop => prop.Name, prop => prop.GetValue(resource, null));

                _configService.SaveConfigDictionary(dictionary);
            }

    }
}
