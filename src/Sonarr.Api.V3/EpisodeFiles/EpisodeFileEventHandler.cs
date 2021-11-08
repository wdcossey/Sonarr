using Microsoft.AspNetCore.SignalR;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.EpisodeFiles
{
    public class EpisodeFileEventHandler : EventHandlerBase<EpisodeFileResource, EpisodeFile>,
        IHandle<EpisodeFileAddedEvent>,
        IHandle<EpisodeFileDeletedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly ISeriesService _seriesService;
        private readonly IUpgradableSpecification _upgradableSpecification;
        
        public EpisodeFileEventHandler(IHubContext<SonarrHub, ISonarrHub> hubContext,
            IMediaFileService mediaFileService,
            ISeriesService seriesService,
            IUpgradableSpecification upgradableSpecification) : base(hubContext)
        {
            _mediaFileService = mediaFileService;
            _seriesService = seriesService;
            _upgradableSpecification = upgradableSpecification;
        }
        
        private EpisodeFileResource GetEpisodeFileResource(int id)
        {
            var episodeFile = _mediaFileService.Get(id);
            var series = _seriesService.GetSeries(episodeFile.SeriesId);
            return episodeFile.ToResource(series, _upgradableSpecification);
        }
        
        void IHandle<EpisodeFileAddedEvent>.Handle(EpisodeFileAddedEvent message)
            => BroadcastResourceChange(ModelAction.Updated, GetResourceById(message.EpisodeFile.Id));

        void IHandle<EpisodeFileDeletedEvent>.Handle(EpisodeFileDeletedEvent message)
            => BroadcastResourceChange(ModelAction.Deleted, GetResourceById(message.EpisodeFile.Id));

        protected override EpisodeFileResource GetResourceById(int id)
        {
            return GetEpisodeFileResource(id);
        }
    }
}
