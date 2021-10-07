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
            //GetResourceAll = GetQueue;
        }

        [HttpGet]
        //[HttpGet("{includeSeries:bool?}/{includeEpisode:bool?}")]
        public List<QueueResource> GetQueue([FromQuery] int? seriesId = null, [FromQuery] string? episodeIds = null, [FromQuery] bool? includeSeries = false, [FromQuery] bool? includeEpisode = true)
        {
            //var includeSeries = Request.GetBooleanQueryParameter("includeSeries");
            //var includeEpisode = Request.GetBooleanQueryParameter("includeEpisode", true);
            var queue = _queueService.GetQueue();
            var pending = _pendingReleaseService.GetPendingQueue();
            var fullQueue = queue.Concat(pending);

            //var seriesIdQuery = Request.Query.SeriesId;
            //var episodeIdsQuery = Request.Query.EpisodeIds;

            if (seriesId.HasValue)
            {
                return fullQueue.Where(q => q.Series?.Id == (int)seriesId).ToResource(includeSeries ?? false, includeEpisode ?? true);
            }

            if (!string.IsNullOrWhiteSpace(episodeIds))
            {
                //string episodeIdsValue = episodeIdsQuery.Value.ToString();

                var episodeIdsList = episodeIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(e => Convert.ToInt32(e))
                                                .ToList();

                return fullQueue.Where(q => q.Episode != null && episodeIdsList.Contains(q.Episode.Id)).ToResource(includeSeries ?? false, includeEpisode ?? true);
            }

            return fullQueue.ToResource(includeSeries ?? false, includeEpisode ?? true);
        }

        public void Handle(QueueUpdatedEvent message)
        {
            //BroadcastResourceChange(ModelAction.Sync);
        }

        public void Handle(PendingReleasesUpdatedEvent message)
        {
            //BroadcastResourceChange(ModelAction.Sync);
        }
    }
}
