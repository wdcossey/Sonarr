using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.Episodes
{
    public class EpisodeEventHandler : EventHandlerBase<EpisodeResource, Episode>,
                                       IHandle<EpisodeGrabbedEvent>,
                                       IHandle<EpisodeImportedEvent>,
                                       IHandle<EpisodeFileDeletedEvent>
    {
        private readonly IEpisodeService _episodeService;

        public EpisodeEventHandler(IHubContext<SonarrHub, ISonarrHub> hubContext, IEpisodeService episodeService)
            : base(hubContext)  => _episodeService = episodeService;

        public void Handle(EpisodeGrabbedEvent message)
        {
            foreach (var episode in message.Episode.Episodes)
            {
                var resource = episode.ToResource();
                resource.Grabbed = true;
                BroadcastResourceChange(ModelAction.Updated, resource);
            }
        }

        public void Handle(EpisodeImportedEvent message)
        {
            foreach (var episode in message.EpisodeInfo.Episodes)
                BroadcastResourceChange(ModelAction.Updated, _episodeService?.GetEpisode(episode.Id)?.ToResource());
        }

        public void Handle(EpisodeFileDeletedEvent message)
        {
            foreach (var episode in message.EpisodeFile.Episodes.Value)
            {
                //TODO: Should this be ModelAction.Deleted?
                BroadcastResourceChange(ModelAction.Updated, _episodeService?.GetEpisode(episode.Id)?.ToResource());
            }
        }

        protected override EpisodeResource GetResourceById(int id)
            => _episodeService.GetEpisode(id).ToResource();
    }
}