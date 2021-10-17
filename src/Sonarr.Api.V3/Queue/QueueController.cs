using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Profiles.Languages;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Queue;
using Sonarr.Http;
using Sonarr.Http.Attributes;
using Sonarr.Http.Extensions;

namespace Sonarr.Api.V3.Queue
{
    [ApiController]
    [SonarrApiRoute("queue", RouteVersion.V3)]
    public class QueueController : ControllerBase, IHandle<QueueUpdatedEvent>, IHandle<PendingReleasesUpdatedEvent>
    {
        private readonly IQueueService _queueService;
        private readonly IPendingReleaseService _pendingReleaseService;

        private readonly LanguageComparer LANGUAGE_COMPARER;
        private readonly QualityModelComparer QUALITY_COMPARER;

        public QueueController(
            //IBroadcastSignalRMessage broadcastSignalRMessage, //TODO: SignalR
            IQueueService queueService,
            IPendingReleaseService pendingReleaseService,
            ILanguageProfileService languageProfileService,
            IQualityProfileService qualityProfileService)
            //: base(broadcastSignalRMessage)
        {
            _queueService = queueService;
            _pendingReleaseService = pendingReleaseService;

            LANGUAGE_COMPARER = new LanguageComparer(languageProfileService.GetDefaultProfile(string.Empty));
            QUALITY_COMPARER = new QualityModelComparer(qualityProfileService.GetDefaultProfile(string.Empty));
        }

        [HttpGet]
        public IActionResult GetQueue([FromQuery] PagingResource<QueueResource> pagingResource, [FromQuery] bool includeUnknownSeriesItems = false, [FromQuery] bool includeSeries = false, [FromQuery] bool includeEpisode = false)
        {
            var pagingSpec = pagingResource.MapToPagingSpec<QueueResource, NzbDrone.Core.Queue.Queue>("timeleft", SortDirection.Ascending);
            return Ok(pagingSpec.ApplyToPage((spec) => GetQueue(spec, includeUnknownSeriesItems), (q) => MapToResource(q, includeSeries, includeEpisode)));
        }

        private PagingSpec<NzbDrone.Core.Queue.Queue> GetQueue(PagingSpec<NzbDrone.Core.Queue.Queue> pagingSpec, bool includeUnknownSeriesItems)
        {
            var ascending = pagingSpec.SortDirection == SortDirection.Ascending;
            var orderByFunc = GetOrderByFunc(pagingSpec);

            var queue = _queueService.GetQueue();
            var filteredQueue = includeUnknownSeriesItems ? queue : queue.Where(q => q.Series != null);
            var pending = _pendingReleaseService.GetPendingQueue();
            var fullQueue = filteredQueue.Concat(pending).ToList();
            IOrderedEnumerable<NzbDrone.Core.Queue.Queue> ordered;

            if (pagingSpec.SortKey == "episode")
            {
                ordered = ascending
                    ? fullQueue.OrderBy(q => q.Episode?.SeasonNumber).ThenBy(q => q.Episode?.EpisodeNumber)
                    : fullQueue.OrderByDescending(q => q.Episode?.SeasonNumber)
                               .ThenByDescending(q => q.Episode?.EpisodeNumber);
            }

            else if (pagingSpec.SortKey == "timeleft")
            {
                ordered = ascending
                    ? fullQueue.OrderBy(q => q.Timeleft, new TimeleftComparer())
                    : fullQueue.OrderByDescending(q => q.Timeleft, new TimeleftComparer());
            }

            else if (pagingSpec.SortKey == "estimatedCompletionTime")
            {
                ordered = ascending
                    ? fullQueue.OrderBy(q => q.EstimatedCompletionTime, new EstimatedCompletionTimeComparer())
                    : fullQueue.OrderByDescending(q => q.EstimatedCompletionTime,
                        new EstimatedCompletionTimeComparer());
            }

            else if (pagingSpec.SortKey == "protocol")
            {
                ordered = ascending
                    ? fullQueue.OrderBy(q => q.Protocol)
                    : fullQueue.OrderByDescending(q => q.Protocol);
            }

            else if (pagingSpec.SortKey == "indexer")
            {
                ordered = ascending
                    ? fullQueue.OrderBy(q => q.Indexer, StringComparer.InvariantCultureIgnoreCase)
                    : fullQueue.OrderByDescending(q => q.Indexer, StringComparer.InvariantCultureIgnoreCase);
            }

            else if (pagingSpec.SortKey == "downloadClient")
            {
                ordered = ascending
                    ? fullQueue.OrderBy(q => q.DownloadClient, StringComparer.InvariantCultureIgnoreCase)
                    : fullQueue.OrderByDescending(q => q.DownloadClient, StringComparer.InvariantCultureIgnoreCase);
            }

            else if (pagingSpec.SortKey == "language")
            {
                ordered = ascending
                    ? fullQueue.OrderBy(q => q.Language, LANGUAGE_COMPARER)
                    : fullQueue.OrderByDescending(q => q.Language, LANGUAGE_COMPARER);
            }

            else if (pagingSpec.SortKey == "quality")
            {
                ordered = ascending
                    ? fullQueue.OrderBy(q => q.Quality, QUALITY_COMPARER)
                    : fullQueue.OrderByDescending(q => q.Quality, QUALITY_COMPARER);
            }

            else
            {
                ordered = ascending ? fullQueue.OrderBy(orderByFunc) : fullQueue.OrderByDescending(orderByFunc);
            }

            ordered = ordered.ThenByDescending(q => q.Size == 0 ? 0 : 100 - q.Sizeleft / q.Size * 100);

            pagingSpec.Records = ordered.Skip((pagingSpec.Page - 1) * pagingSpec.PageSize).Take(pagingSpec.PageSize).ToList();
            pagingSpec.TotalRecords = fullQueue.Count;

            if (pagingSpec.Records.Empty() && pagingSpec.Page > 1)
            {
                pagingSpec.Page = (int)Math.Max(Math.Ceiling((decimal)(pagingSpec.TotalRecords / pagingSpec.PageSize)), 1);
                pagingSpec.Records = ordered.Skip((pagingSpec.Page - 1) * pagingSpec.PageSize).Take(pagingSpec.PageSize).ToList();
            }

            return pagingSpec;
        }

        private Func<NzbDrone.Core.Queue.Queue, Object> GetOrderByFunc(PagingSpec<NzbDrone.Core.Queue.Queue> pagingSpec)
        {
            switch (pagingSpec.SortKey)
            {
                case "status":
                    return q => q.Status;
                case "series.sortTitle":
                    return q => q.Series?.SortTitle ?? string.Empty;
                case "title":
                    return q => q.Title;
                case "episode":
                    return q => q.Episode;
                case "episode.airDateUtc":
                    return q => q.Episode?.AirDateUtc ?? DateTime.MinValue;
                case "episode.title":
                    return q => q.Episode?.Title ?? string.Empty;
                case "language":
                    return q => q.Language;
                case "quality":
                    return q => q.Quality;
                case "progress":
                    // Avoid exploding if a download's size is 0
                    return q => 100 - q.Sizeleft / Math.Max(q.Size * 100, 1);
                default:
                    return q => q.Timeleft;
            }
        }

        private QueueResource MapToResource(NzbDrone.Core.Queue.Queue queueItem, bool includeSeries, bool includeEpisode)
            => queueItem.ToResource(includeSeries, includeEpisode);

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