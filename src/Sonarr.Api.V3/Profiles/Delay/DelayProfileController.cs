using System;
using System.Collections.Generic;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Nancy;
using NzbDrone.Core.Profiles.Delay;
using Sonarr.Http;
using Sonarr.Http.REST;
using Sonarr.Http.Validation;

namespace Sonarr.Api.V3.Profiles.Delay
{
    [ApiController]
    [SonarrV3Route("delayprofile")]
    public class DelayProfileController : ControllerBase
    {
        private readonly IDelayProfileService _delayProfileService;

        public DelayProfileController(IDelayProfileService delayProfileService, DelayProfileTagInUseValidator tagInUseValidator)
        {
            _delayProfileService = delayProfileService;

            /*GetResourceAll = GetAll;
            GetResourceById = GetById;
            UpdateResource = Update;
            CreateResource = Create;
            DeleteResource = DeleteProfile;
            Put(@"/reorder/(?<id>[\d]{1,10})",  options => Reorder(options.Id));

            SharedValidator.RuleFor(d => d.Tags).NotEmpty().When(d => d.Id != 1);
            SharedValidator.RuleFor(d => d.Tags).EmptyCollection<DelayProfileResource, int>().When(d => d.Id == 1);
            SharedValidator.RuleFor(d => d.Tags).SetValidator(tagInUseValidator);
            SharedValidator.RuleFor(d => d.UsenetDelay).GreaterThanOrEqualTo(0);
            SharedValidator.RuleFor(d => d.TorrentDelay).GreaterThanOrEqualTo(0);

            SharedValidator.RuleFor(d => d).Custom((delayProfile, context) =>
            {
                if (!delayProfile.EnableUsenet && !delayProfile.EnableTorrent)
                {
                    context.AddFailure("Either Usenet or Torrent should be enabled");
                }
            });*/
        }

        [HttpPost]
        public IActionResult Create([FromBody] DelayProfileResource resource)
        {
            var model = resource.ToModel();
            model = _delayProfileService.Add(model);
            return Created($"{Request.Path}/{model.Id}", model);
        }

        [HttpDelete("{id:int}")]
        public IActionResult DeleteProfile(int id)
        {
            if (id == 1)
                throw new MethodNotAllowedException("Cannot delete global delay profile");

            _delayProfileService.Delete(id);

            return Ok(new object());
        }

        [HttpPut]
        //[HttpPut("{id:int?}")]
        public IActionResult Update([FromBody] DelayProfileResource resource)
        {
            var model = resource.ToModel();
            return Accepted(_delayProfileService.Update(model));
        }

        [HttpGet("{id:int:required}")]
        public IActionResult GetById(int id)
            => Ok(_delayProfileService.Get(id).ToResource());

        [HttpGet]
        public IActionResult GetAll()
            => Ok(_delayProfileService.All().ToResource());

        [HttpPut("reorder/{id:int:required}")]
        public IActionResult Reorder(int id, [FromQuery] int? after)
        {
            //ValidateId(id);

            //var afterIdQuery = Request.Query.After;
            //int? afterId = afterIdQuery.HasValue ? Convert.ToInt32(afterIdQuery.Value) : null;

            return Ok(_delayProfileService.Reorder(id, after).ToResource());
        }
    }
}