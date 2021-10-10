using System.Collections.Generic;
using NzbDrone.Core.ImportLists.Exclusions;
using Sonarr.Http;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Validation;

namespace Sonarr.Api.V3.ImportLists
{
    [ApiController]
    [SonarrV3Route("importlistexclusion")]
    public class ImportListExclusionController : ControllerBase
    {
        private readonly IImportListExclusionService _importListExclusionService;

        public ImportListExclusionController(IImportListExclusionService importListExclusionService,
                                         ImportListExclusionExistsValidator importListExclusionExistsValidator)
        {
            _importListExclusionService = importListExclusionService;

            /*GetResourceById = GetImportListExclusion;
            GetResourceAll = GetImportListExclusions;
            CreateResource = AddImportListExclusion;
            UpdateResource = UpdateImportListExclusion;
            DeleteResource = DeleteImportListExclusionResource;

            SharedValidator.RuleFor(c => c.TvdbId).NotEmpty().SetValidator(importListExclusionExistsValidator);
            SharedValidator.RuleFor(c => c.Title).NotEmpty();*/
        }

        [HttpGet("{id:int:required}")]
        public IActionResult GetImportListExclusion(int id)
            => Ok(_importListExclusionService.Get(id).ToResource());

        public IActionResult GetImportListExclusions()
            => Ok(_importListExclusionService.All().ToResource());

        [HttpPost]
        public IActionResult AddImportListExclusion([FromBody] ImportListExclusionResource resource)
        {
            var model = _importListExclusionService.Add(resource.ToModel());
            return Created($"{Request.Path}/{model.Id}", model.ToResource());
        }

        [HttpPut]
        public IActionResult UpdateImportListExclusion([FromBody] ImportListExclusionResource resource)
        {
            var model = _importListExclusionService.Update(resource.ToModel());
            return Accepted(model.ToResource());
        }

        [HttpDelete("{id:int:required}")]
        public IActionResult DeleteImportListExclusionResource(int id)
        {
            _importListExclusionService.Delete(id);
            return Ok(new object());
        }
    }
}
