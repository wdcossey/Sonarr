using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Tags;

namespace Sonarr.Blazor.Server.Controllers.v3.Tags
{
    [ApiController]
    [Route("/api/v3/tag/detail")]
    public class TagDetailsController : ControllerBase// SonarrRestModule<TagDetailsResource>
    {
        private readonly ITagService _tagService;

        public TagDetailsController(ITagService tagService)
        {
            _tagService = tagService;

            /*GetResourceById = GetTagDetails;
            GetResourceAll = GetAll;*/
        }

        [HttpGet("{id:int}")]
        public TagDetailsResource GetTagDetails(int id)
        {
            return _tagService.Details(id).ToResource();
        }

        [HttpGet]
        public List<TagDetailsResource> GetAll()
        {
            return _tagService.Details().ToResource();
        }
    }
}
