using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Queue;

namespace Sonarr.Api.V3.Queue
{
    [ApiController]
    [Route("/api/v3/queue/details")]
    public class QueueDetailsController : ControllerBase, IHandle<QueueUpdatedEvent>, IHandle<PendingReleasesUpdatedEvent>//SonarrRestModuleWithSignalR<QueueResourceNzbDrone.Core.Queue.Queue>,
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
        //[HttpGet("{includeSeries:bool?}/{includeEpisode:bool?}")]
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

        public void Handle(QueueUpdatedEvent message)
        {
            //TODO: SignalR
            //BroadcastResourceChange(ModelAction.Sync);
        }

        public void Handle(PendingReleasesUpdatedEvent message)
        {
            //TODO: SignalR
            //BroadcastResourceChange(ModelAction.Sync);
        }
    }
}
