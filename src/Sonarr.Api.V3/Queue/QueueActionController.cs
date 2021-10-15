using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Queue;
using Sonarr.Http.Attributes;
using Sonarr.Http.REST;

namespace Sonarr.Api.V3.Queue
{
    [ApiController]
    [SonarrApiRoute("queue", RouteVersion.V3)]
    public class QueueActionController : ControllerBase
    {
        private readonly IQueueService _queueService;
        private readonly ITrackedDownloadService _trackedDownloadService;
        private readonly IFailedDownloadService _failedDownloadService;
        private readonly IIgnoredDownloadService _ignoredDownloadService;
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly IPendingReleaseService _pendingReleaseService;
        private readonly IDownloadService _downloadService;
        private readonly IBlocklistService _blocklistService;

        public QueueActionController(IQueueService queueService,
                                 ITrackedDownloadService trackedDownloadService,
                                 IFailedDownloadService failedDownloadService,
                                 IIgnoredDownloadService ignoredDownloadService,
                                 IProvideDownloadClient downloadClientProvider,
                                 IPendingReleaseService pendingReleaseService,
                                 IDownloadService downloadService,
                                 IBlocklistService blocklistService)
        {
            _queueService = queueService;
            _trackedDownloadService = trackedDownloadService;
            _failedDownloadService = failedDownloadService;
            _ignoredDownloadService = ignoredDownloadService;
            _downloadClientProvider = downloadClientProvider;
            _pendingReleaseService = pendingReleaseService;
            _downloadService = downloadService;
            _blocklistService = blocklistService;

            //Post(@"/grab/(?<id>[\d]{1,10})",  x => Grab((int)x.Id));
            //Post("/grab/bulk",  x => Grab());

            //Delete(@"/(?<id>[\d]{1,10})",  x => Remove((int)x.Id));
            //Delete("/bulk",  x => Remove());
        }

        [HttpPost("grab/{id:int:required}")]
        public IActionResult Grab(int id)
        {
            var pendingRelease = _pendingReleaseService.FindPendingQueueItem(id);

            if (pendingRelease == null)
                return NotFound();

            _downloadService.DownloadReport(pendingRelease.RemoteEpisode);

            return Ok(new object());
        }

        [HttpPost("grab/bulk")]
        public IActionResult Grab([FromBody] QueueBulkResource resource) //TODO: FromBody or FromForm (some UI requests are broken)?!?
        {
            //var resource = Request.Body.FromJson<QueueBulkResource>();

            foreach (var id in resource.Ids)
            {
                var pendingRelease = _pendingReleaseService.FindPendingQueueItem(id);

                if (pendingRelease == null)
                    return NotFound();

                _downloadService.DownloadReport(pendingRelease.RemoteEpisode);
            }

            return Ok(new object());
        }

        [HttpPost("grab/bulk")]
        [Consumes("application/x-www-form-urlencoded")] //TODO: some UI requests are broken?!?
        public IActionResult GrabFormForm([FromForm] QueueBulkResource resource)
            => Grab(resource);

        [HttpDelete("{id:int:required}")]
        public IActionResult Remove(
            int id,
            [FromQuery] bool removeFromClient = true,
            [FromQuery] bool? blocklist = false,
            [FromQuery] bool? blacklist = false)
        {
            // blacklist maintained for backwards compatability, UI uses blocklist.
            var trackedDownload = Remove(id, removeFromClient, (blocklist is true || blacklist is true));

            if (trackedDownload != null)
                _trackedDownloadService.StopTracking(trackedDownload.DownloadItem.DownloadId);

            return Ok(new object());
        }

        [HttpDelete("bulk")]
        public IActionResult Remove(
            [FromBody] QueueBulkResource resource,
            [FromQuery] bool removeFromClient = true,
            [FromQuery] bool? blocklist = false,
            [FromQuery] bool? blacklist = false) //TODO: FromBody or FromForm (some UI requests are broken)?!?
        {
            //var removeFromClient = Request.GetBooleanQueryParameter("removeFromClient", true);

            // blacklist maintained for backwards compatability, UI uses blocklist.
            //var blocklist = Request.GetBooleanQueryParameter("blocklist") ? Request.GetBooleanQueryParameter("blocklist") : Request.GetBooleanQueryParameter("blacklist");

            //var resource = Request.Body.FromJson<QueueBulkResource>();
            var trackedDownloadIds = new List<string>();

            foreach (var id in resource.Ids)
            {
                var trackedDownload = Remove(id, removeFromClient, (blocklist is true || blacklist is true));

                if (trackedDownload != null)
                {
                    trackedDownloadIds.Add(trackedDownload.DownloadItem.DownloadId);
                }
            }

            _trackedDownloadService.StopTracking(trackedDownloadIds);

            return Ok(new object());
        }

        [HttpDelete("bulk")]
        [Consumes("application/x-www-form-urlencoded")] //TODO: some UI requests are broken?!?
        public IActionResult RemoveFromForm(
            [FromForm] QueueBulkResource resource,
            [FromQuery] bool removeFromClient = true,
            [FromQuery] bool? blocklist = false,
            [FromQuery] bool? blacklist = false)
            => Remove(resource, removeFromClient, blocklist, blacklist);

        private TrackedDownload Remove(int id, bool removeFromClient, bool blocklist)
        {
            var pendingRelease = _pendingReleaseService.FindPendingQueueItem(id);

            if (pendingRelease != null)
            {
                _blocklistService.Block(pendingRelease.RemoteEpisode, "Pending release manually blocklisted");
                _pendingReleaseService.RemovePendingQueueItems(pendingRelease.Id);

                return null;
            }

            var trackedDownload = GetTrackedDownload(id);

            if (trackedDownload == null)
                throw new NotFoundException();

            if (removeFromClient)
            {
                var downloadClient = _downloadClientProvider.Get(trackedDownload.DownloadClient);

                if (downloadClient == null)
                    throw new BadRequestException();

                downloadClient.RemoveItem(trackedDownload.DownloadItem, true);
            }

            if (blocklist)
            {
                _failedDownloadService.MarkAsFailed(trackedDownload.DownloadItem.DownloadId);
            }

            if (!removeFromClient && !blocklist)
            {
                if (!_ignoredDownloadService.IgnoreDownload(trackedDownload))
                {
                    return null;
                }
            }

            return trackedDownload;
        }

        private TrackedDownload GetTrackedDownload(int queueId)
        {
            var queueItem = _queueService.Find(queueId);

            if (queueItem == null)
                throw new NotFoundException();

            var trackedDownload = _trackedDownloadService.Find(queueItem.DownloadId);

            if (trackedDownload == null)
                throw new NotFoundException();

            return trackedDownload;
        }
    }
}
