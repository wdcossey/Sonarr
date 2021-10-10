using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Qualities;

namespace Sonarr.Api.V3.Qualities
{
    [ApiController]
    [SonarrV3Route("qualitydefinition")]
    public class QualityDefinitionController : ControllerBase
    {
        private readonly IQualityDefinitionService _qualityDefinitionService;

        public QualityDefinitionController(IQualityDefinitionService qualityDefinitionService)
            => _qualityDefinitionService = qualityDefinitionService;

        [HttpPut]
        public IActionResult Update([FromBody] QualityDefinitionResource resource)
        {
            var model = resource.ToModel();
            var  result = _qualityDefinitionService.Update(model);
            return Accepted(result.ToResource());
        }

        [HttpGet("{id:int:required}")]
        public IActionResult GetById(int id)
            => Ok(_qualityDefinitionService.GetById(id).ToResource());

        [HttpGet]
        public IActionResult GetAll()
            => Ok(_qualityDefinitionService.All().ToResource());

        [HttpPut("update")]
        public IActionResult UpdateMany([FromBody] List<QualityDefinitionResource> qualityDefinitions)
        {
            _qualityDefinitionService.UpdateMany(qualityDefinitions.ToModel().ToList());
            return Accepted(_qualityDefinitionService.All().ToResource());
        }
    }
}