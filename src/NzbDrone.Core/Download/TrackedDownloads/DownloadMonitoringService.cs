using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Download.TrackedDownloads
{
    public class DownloadMonitoringService : IExecuteAsync<RefreshMonitoredDownloadsCommand>,
                                             IExecuteAsync<CheckForFinishedDownloadCommand>,
                                             IHandleAsync<EpisodeGrabbedEvent>,
                                             IHandleAsync<EpisodeImportedEvent>,
                                             IHandleAsync<DownloadsProcessedEvent>,
                                             IHandleAsync<TrackedDownloadsRemovedEvent>
    {
        private readonly IDownloadClientStatusService _downloadClientStatusService;
        private readonly IDownloadClientFactory _downloadClientFactory;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _manageCommandQueue;
        private readonly IConfigService _configService;
        private readonly IFailedDownloadService _failedDownloadService;
        private readonly ICompletedDownloadService _completedDownloadService;
        private readonly ITrackedDownloadService _trackedDownloadService;
        private readonly ILogger<DownloadMonitoringService> _logger;
        private readonly Debouncer _refreshDebounce;

        public DownloadMonitoringService(IDownloadClientStatusService downloadClientStatusService,
                                         IDownloadClientFactory downloadClientFactory,
                                         IEventAggregator eventAggregator,
                                         IManageCommandQueue manageCommandQueue,
                                         IConfigService configService,
                                         IFailedDownloadService failedDownloadService,
                                         ICompletedDownloadService completedDownloadService,
                                         ITrackedDownloadService trackedDownloadService,
                                         ILogger<DownloadMonitoringService> logger)
        {
            _downloadClientStatusService = downloadClientStatusService;
            _downloadClientFactory = downloadClientFactory;
            _eventAggregator = eventAggregator;
            _manageCommandQueue = manageCommandQueue;
            _configService = configService;
            _failedDownloadService = failedDownloadService;
            _completedDownloadService = completedDownloadService;
            _trackedDownloadService = trackedDownloadService;
            _logger = logger;

            _refreshDebounce = new Debouncer(QueueRefresh, TimeSpan.FromSeconds(5));
        }

        private void QueueRefresh()
        {
            _manageCommandQueue.Push(new RefreshMonitoredDownloadsCommand());
        }

        private void Refresh()
        {
            _refreshDebounce.Pause();
            try
            {
                var downloadClients = _downloadClientFactory.DownloadHandlingEnabled();

                var trackedDownloads = new List<TrackedDownload>();

                foreach (var downloadClient in downloadClients)
                {
                    var clientTrackedDownloads = ProcessClientDownloads(downloadClient);

                    trackedDownloads.AddRange(clientTrackedDownloads.Where(DownloadIsTrackable));
                }

                _trackedDownloadService.UpdateTrackable(trackedDownloads);
                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(trackedDownloads));
                _manageCommandQueue.Push(new ProcessMonitoredDownloadsCommand());
            }
            finally
            {
                _refreshDebounce.Resume();
            }
        }

        private List<TrackedDownload> ProcessClientDownloads(IDownloadClient downloadClient)
        {
            var downloadClientItems = new List<DownloadClientItem>();
            var trackedDownloads = new List<TrackedDownload>();

            try
            {
                downloadClientItems = downloadClient.GetItems().ToList();

                _downloadClientStatusService.RecordSuccess(downloadClient.Definition.Id);
            }
            catch (Exception ex)
            {
                // TODO: Stop tracking items for the offline client

                _downloadClientStatusService.RecordFailure(downloadClient.Definition.Id);
                _logger.LogWarning(ex, "Unable to retrieve queue and history items from {DefinitionName}", downloadClient.Definition.Name);
            }

            foreach (var downloadItem in downloadClientItems)
            {
                var item = ProcessClientItem(downloadClient, downloadItem);
                trackedDownloads.AddIfNotNull(item);
            }

            return trackedDownloads;
        }

        private TrackedDownload ProcessClientItem(IDownloadClient downloadClient, DownloadClientItem downloadItem)
        {
            try
            {
                var trackedDownload = _trackedDownloadService.TrackDownload((DownloadClientDefinition)downloadClient.Definition, downloadItem);

                if (trackedDownload != null && trackedDownload.State == TrackedDownloadState.Downloading)
                {
                    _failedDownloadService.Check(trackedDownload);
                    _completedDownloadService.Check(trackedDownload);
                }

                return trackedDownload;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Couldn't process tracked download {Title}", downloadItem.Title);
            }

            return null;
        }

        private bool DownloadIsTrackable(TrackedDownload trackedDownload)
        {
            // If the download has already been imported, failed or the user ignored it don't track it
            if (trackedDownload.State == TrackedDownloadState.Imported ||
                trackedDownload.State == TrackedDownloadState.Failed ||
                trackedDownload.State == TrackedDownloadState.Ignored)
            {
                return false;
            }

            // If CDH is disabled and the download status is complete don't track it
            if (!_configService.EnableCompletedDownloadHandling && trackedDownload.DownloadItem.Status == DownloadItemStatus.Completed)
            {
                return false;
            }

            return true;
        }

        public Task ExecuteAsync(RefreshMonitoredDownloadsCommand message)
        {
            Refresh();
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(CheckForFinishedDownloadCommand message)
        {
            _logger.LogWarning("A third party app used the deprecated CheckForFinishedDownload command, it should be updated RefreshMonitoredDownloads instead");
            Refresh();
            return Task.CompletedTask;
        }

        public Task HandleAsync(EpisodeGrabbedEvent message)
        {
            _refreshDebounce.Execute();
            return Task.CompletedTask;
        }

        public Task HandleAsync(EpisodeImportedEvent message)
        {
            _refreshDebounce.Execute();
            return Task.CompletedTask;
        }

        public Task HandleAsync(DownloadsProcessedEvent message)
        {
            var trackedDownloads = _trackedDownloadService.GetTrackedDownloads().Where(t => t.IsTrackable && DownloadIsTrackable(t)).ToList();

            _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(trackedDownloads));
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(TrackedDownloadsRemovedEvent message)
        {
            var trackedDownloads = _trackedDownloadService.GetTrackedDownloads().Where(t => t.IsTrackable && DownloadIsTrackable(t)).ToList();

            _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(trackedDownloads));
            
            return Task.CompletedTask;
        }
    }
}
