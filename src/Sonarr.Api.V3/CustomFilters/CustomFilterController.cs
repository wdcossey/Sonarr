using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.CustomFilters;

namespace Sonarr.Api.V3.CustomFilters
{
    [ApiController]
    [Route("/api/v3/customFilter")]
    public class CustomFilterController : ControllerBase
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

        [HttpGet("{id:int}")]
        public CustomFilterResource GetCustomFilter(int id)
        {
            return _customFilterService.Get(id).ToResource();
        }

        [HttpGet]
        public List<CustomFilterResource> GetCustomFilters()
        {
            return _customFilterService.All().ToResource();
        }

        private int AddCustomFilter(CustomFilterResource resource)
        {
            var customFilter = _customFilterService.Add(resource.ToModel());

            return customFilter.Id;
        }

        private void UpdateCustomFilter(CustomFilterResource resource)
        {
            _customFilterService.Update(resource.ToModel());
        }

        private void DeleteCustomResource(int id)
        {
            _customFilterService.Delete(id);
        }
    }
}
