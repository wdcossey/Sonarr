using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Tags;

namespace Sonarr.Blazor.Server.Controllers.v3.Tags
{
    [ApiController]
    [Route("/api/v3/tag")]
    public class TagController : ControllerBase//SonarrRestModuleWithSignalR<TagResource, Tag>, IHandle<TagsUpdatedEvent>
    {
        private readonly ITagService _tagService;

        public TagController(
            //IBroadcastSignalRMessage signalRBroadcaster,
            ITagService tagService)
            //: base(signalRBroadcaster)
        {
            _tagService = tagService;

            /*GetResourceById = GetTag;
            GetResourceAll = GetAll;
            CreateResource = Create;
            UpdateResource = Update;
            DeleteResource = DeleteTag;*/
        }

        [HttpGet("{id:int}")]
        private TagResource GetTag(int id)
        {
            return _tagService.GetTag(id).ToResource();
        }

        [HttpGet]
        public List<TagResource> GetAll()
        {
            return _tagService.All().ToResource();
        }

        [HttpPost]
        public int Create([FromBody] TagResource resource)
        {
            return _tagService.Add(resource.ToModel()).Id;
        }

        [HttpPut]
        public void Update([FromBody] TagResource resource)
        {
            _tagService.Update(resource.ToModel());
        }

        [HttpDelete("{id:int}")]
        public void DeleteTag(int id)
        {
            _tagService.Delete(id);
        }

        /*public void Handle(TagsUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Sync);
        }*/
    }
}
