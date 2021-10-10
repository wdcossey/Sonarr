using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.CustomFilters;

namespace Sonarr.Api.V3.CustomFilters
{
    [ApiController]
    [SonarrV3Route("customFilter")]
    public class CustomFilterController : SonarrControllerBase<CustomFilterResource>
    {
        private readonly ICustomFilterService _customFilterService;

        public CustomFilterController(ICustomFilterService customFilterService)
        {
            _customFilterService = customFilterService;

            /*GetResourceById = GetCustomFilter;
            GetResourceAll = GetCustomFilters;
            CreateResource = AddCustomFilter;
            UpdateResource = UpdateCustomFilter;
            DeleteResource = DeleteCustomResource;*/
        }

        protected override Task<IList<CustomFilterResource>> GetAllResourcesAsync()
            => Task.FromResult<IList<CustomFilterResource>>(_customFilterService.All().ToResource());

        protected override Task<CustomFilterResource> GetResourceByIdAsync(int id)
            => Task.FromResult(_customFilterService.Get(id).ToResource());

        protected override Task DeleteResourceByIdAsync(int id)
        {
            _customFilterService.Delete(id);
            return Task.CompletedTask;
        }

        protected override Task<CustomFilterResource> UpdateResourceAsync(CustomFilterResource resource)
        {
            var result = _customFilterService.Update(resource.ToModel());
            return Task.FromResult(GetCustomFilter(result.Id));
        }

        protected override Task<CustomFilterResource> CreateResourceAsync(CustomFilterResource resource)
        {
            var customFilter = _customFilterService.Add(resource.ToModel());
            return Task.FromResult(GetCustomFilter(customFilter.Id));
        }

        private CustomFilterResource GetCustomFilter(int id)
            => _customFilterService.Get(id).ToResource();
    }
}
