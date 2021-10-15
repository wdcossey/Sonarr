﻿using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Profiles.Releases;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Profiles.Release
{
    [ApiController]
    [SonarrApiRoute("releaseprofile", RouteVersion.V3)]
    public class ReleaseProfileController : ControllerBase
    {
        private readonly IReleaseProfileService _releaseProfileService;
        private readonly IIndexerFactory _indexerFactory;

        public ReleaseProfileController(IReleaseProfileService releaseProfileService, IIndexerFactory indexerFactory)
        {
            _releaseProfileService = releaseProfileService;
            _indexerFactory = indexerFactory;

            /*SharedValidator.RuleFor(d => d).Custom((restriction, context) =>
            {
                if (restriction.Ignored.Empty() && restriction.Required.Empty() && restriction.Preferred.Empty())
                {
                    context.AddFailure("'Must contain', 'Must not contain' or 'Preferred' is required");
                }

                if (restriction.Enabled && restriction.IndexerId != 0 && !_indexerFactory.Exists(restriction.IndexerId))
                {
                    context.AddFailure(nameof(ReleaseProfile.IndexerId), "Indexer does not exist");
                }

                if (restriction.Preferred.Any(p => p.Key.IsNullOrWhiteSpace()))
                {
                    context.AddFailure("Preferred", "Term cannot be empty or consist of only spaces");
                }
            });*/
        }

        [HttpGet("{id:int:required}")]
        public IActionResult GetReleaseProfile(int id)
            => Ok(_releaseProfileService.Get(id).ToResource());

        [HttpGet]
        public IActionResult GetAll()
            => Ok(_releaseProfileService.All().ToResource());

        [HttpPost]
        public IActionResult Create([FromBody] ReleaseProfileResource resource)
        {
            var model = _releaseProfileService.Add(resource.ToModel());
            return Created($"{Request.Path}/{model.Id}", model.ToResource());
        }

        [HttpPut]
        [HttpPut("{id:int?}")]
        public IActionResult Update(int? id, [FromBody] ReleaseProfileResource resource)
            => Accepted(_releaseProfileService.Update(resource.ToModel()).ToResource());

        [HttpDelete("{id:int:required}")]
        public IActionResult DeleteReleaseProfile(int id)
        {
            _releaseProfileService.Delete(id);
            return Ok(new object());
        }
    }
}