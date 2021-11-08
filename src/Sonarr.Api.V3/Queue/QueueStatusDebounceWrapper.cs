using System;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Queue;
using NzbDrone.SignalR;
using Sonarr.Http;

namespace Sonarr.Api.V3.Queue
{
    public interface IQueueStatusDebounceWrapper
    {
        QueueStatusResource GetQueueStatus();

        void Execute();
    }
    
    public class QueueStatusDebounceWrapper : EventHandlerBase<QueueStatusResource>, IQueueStatusDebounceWrapper
    {
        private readonly IQueueService _queueService;
        private readonly IPendingReleaseService _pendingReleaseService;
        private readonly Debouncer _broadcastDebounce;

        public QueueStatusDebounceWrapper(
            IHubContext<SonarrHub, ISonarrHub> hubContext,
            IQueueService queueService,
            IPendingReleaseService pendingReleaseService) : base(hubContext)
        {
            _queueService = queueService;
            _pendingReleaseService = pendingReleaseService;
            _broadcastDebounce = new Debouncer(BroadcastChange, TimeSpan.FromSeconds(5));
        }
        
        private void BroadcastChange()
            => BroadcastResourceChange(ModelAction.Updated, GetQueueStatus());

        public QueueStatusResource GetQueueStatus()
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

        public void Execute()
            => _broadcastDebounce.Execute();
    }
}
