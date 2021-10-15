using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Qualities;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Qualities
{
    [ApiController]
    [SonarrApiRoute("qualitydefinition", RouteVersion.V3)]
    public class QualityDefinitionController : ControllerBase
    {
        private readonly IQualityDefinitionService _qualityDefinitionService;

        public QualityDefinitionController(IQualityDefinitionService qualityDefinitionService)
            => _qualityDefinitionService = qualityDefinitionService;

        [HttpPut]
        [HttpPut("{id:int?}")]
        public IActionResult Update(int? id, [FromBody] QualityDefinitionResource resource)
            => Accepted(_qualityDefinitionService.Update(resource.ToModel()).ToResource());

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