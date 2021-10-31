using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Queue;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Queue
{
    [ApiController]
    [SonarrApiRoute("queue/details", RouteVersion.V3)]
    public class QueueDetailsController : ControllerBase, IHandle<QueueUpdatedEvent>, IHandle<PendingReleasesUpdatedEvent>
    {
        private readonly IQueueService _queueService;
        private readonly IPendingReleaseService _pendingReleaseService;

        public QueueDetailsController(
            //IBroadcastSignalRMessage broadcastSignalRMessage,
            IQueueService queueService,
            IPendingReleaseService pendingReleaseService)
            //: base(broadcastSignalRMessage, "queue/details")
        {
            _queueService = queueService;
            _pendingReleaseService = pendingReleaseService;
        }

        [HttpGet]
        public IActionResult GetQueue(
            [FromQuery] int? seriesId = null,
            [FromQuery] IList<int> episodeIds = null,
            [FromQuery] bool includeSeries = false,
            [FromQuery] bool includeEpisode = true)
        {
            var queue = _queueService.GetQueue();
            var pending = _pendingReleaseService.GetPendingQueue();
            var fullQueue = queue.Concat(pending);

            if (seriesId.HasValue)
                return Ok(fullQueue.Where(q => q.Series?.Id == (int)seriesId).ToResource(includeSeries, includeEpisode));

            if (episodeIds?.Any() == true)
                return Ok(fullQueue.Where(q => q.Episode != null && episodeIds.Contains(q.Episode.Id)).ToResource(includeSeries, includeEpisode));

            return Ok(fullQueue.ToResource(includeSeries, includeEpisode));
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public void Handle(QueueUpdatedEvent message)
        {
            //TODO: SignalR
            //BroadcastResourceChange(ModelAction.Sync);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public void Handle(PendingReleasesUpdatedEvent message)
        {
            //TODO: SignalR
            //BroadcastResourceChange(ModelAction.Sync);
        }
    }
}
