using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tags;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.Tags
{
    public class TagEventHandler : EventHandlerBase<TagResource, Tag>, IHandle<TagsUpdatedEvent>
    {
        private readonly ITagService _tagService;

        public TagEventHandler(IHubContext<SonarrHub, ISonarrHub> hubContext, ITagService tagService) 
            : base(hubContext) => _tagService = tagService;

        public void Handle(TagsUpdatedEvent message)
            => BroadcastResourceChange(ModelAction.Sync);

        protected override TagResource GetResourceById(int id)
            => _tagService.GetTag(id).ToResource();
    }
}
