﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tags;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.Tags
{
    [ApiController]
    [SonarrApiRoute("tag", RouteVersion.V3)]
    public class TagController : ControllerBase, IHandle<TagsUpdatedEvent>
    {
        private readonly ITagService _tagService;

        public TagController(
                //IBroadcastSignalRMessage signalRBroadcaster, //TODO: SignalR Hub
                ITagService tagService)
            //: base(signalRBroadcaster)
        {
            _tagService = tagService;
        }

        [HttpGet("{id:int:required}")]
        public IActionResult GetTag(int id)
            => Ok(_tagService.GetTag(id).ToResource());

        [HttpGet]
        public IActionResult GetAll()
            => Ok(_tagService.All().ToResource());

        [HttpPost]
        public IActionResult Create([FromBody] TagResource resource)
        {
            var tag = _tagService.Add(resource.ToModel());
            return Created($"{Request.Path}/{tag.Id}", tag.ToResource());
        }

        [HttpPut]
        public IActionResult Update([FromBody] TagResource resource)
        {
            var tag =_tagService.Update(resource.ToModel());
            return Accepted(tag.ToResource());
        }

        [HttpDelete("{id:int:required}")]
        public IActionResult DeleteTag(int id)
        {
            _tagService.Delete(id);
            return Ok(new object());
        }

        public void Handle(TagsUpdatedEvent message)
        {
            //TODO: SignalR Hub
            //BroadcastResourceChange(ModelAction.Sync);
        }
    }
}
