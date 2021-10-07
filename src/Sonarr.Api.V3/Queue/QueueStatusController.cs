using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Queue;

namespace Sonarr.Api.V3.Queue
{
    [ApiController]
    [Route("/api/v3/queue/status")]
    public class QueueStatusController : ControllerBase, IHandle<QueueUpdatedEvent>, IHandle<PendingReleasesUpdatedEvent>//SonarrRestModuleWithSignalR<QueueStatusResource, NzbDrone.Core.Queue.Queue>,
    {
        private readonly IQueueService _queueService;
        private readonly IPendingReleaseService _pendingReleaseService;
        private readonly Debouncer _broadcastDebounce;


        public QueueStatusController(
            //IBroadcastSignalRMessage broadcastSignalRMessage,
            IQueueService queueService,
            IPendingReleaseService pendingReleaseService)
            //: base(broadcastSignalRMessage, "queue/status")
        {
            _queueService = queueService;
            _pendingReleaseService = pendingReleaseService;

            _broadcastDebounce = new Debouncer(BroadcastChange, TimeSpan.FromSeconds(5));

            //Get("/",  x => GetQueueStatusResponse());
        }

        [HttpGet]
        public object GetQueueStatusResponse()
        {
            return GetQueueStatus();
        }

        private QueueStatusResource GetQueueStatus()
        {
            _broadcastDebounce.Pause();

            var queue = _queueService.GetQueue();
            var pending = _pendingReleaseService.GetPendingQueue();

            var resource = new QueueStatusResource
            {
                TotalCount = queue.Count + pending.Count,
                Count = queue.Count(q => q.Series != null) + pending.Count,
                UnknownCount = queue.Count(q => q.Series == null),
                Errors = queue.Any(q => q.Series != null && q.TrackedDownloadStatus == TrackedDownloadStatus.Error),
                Warnings = queue.Any(q => q.Series != null && q.TrackedDownloadStatus == TrackedDownloadStatus.Warning),
                UnknownErrors = queue.Any(q => q.Series == null && q.TrackedDownloadStatus == TrackedDownloadStatus.Error),
                UnknownWarnings = queue.Any(q => q.Series == null && q.TrackedDownloadStatus == TrackedDownloadStatus.Warning)
            };

            _broadcastDebounce.Resume();

            return resource;
        }

        private void BroadcastChange()
        {
            //BroadcastResourceChange(ModelAction.Updated, GetQueueStatus());
        }

        public void Handle(QueueUpdatedEvent message)
        {
            _broadcastDebounce.Execute();
        }

        public void Handle(PendingReleasesUpdatedEvent message)
        {
            _broadcastDebounce.Execute();
        }
    }
}
