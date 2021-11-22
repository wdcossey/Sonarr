﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Download
{
    public class DownloadEventHub : IHandleAsync<DownloadFailedEvent>,
                                    IHandleAsync<DownloadCompletedEvent>,
                                    IHandleAsync<DownloadCanBeRemovedEvent>
    {
        private readonly IConfigService _configService;
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly ILogger<DownloadEventHub> _logger;

        public DownloadEventHub(IConfigService configService,
            IProvideDownloadClient downloadClientProvider,
            ILogger<DownloadEventHub> logger)
        {
            _configService = configService;
            _downloadClientProvider = downloadClientProvider;
            _logger = logger;
        }

        public Task HandleAsync(DownloadFailedEvent message)
        {
            var trackedDownload = message.TrackedDownload;

            if (trackedDownload == null ||
                message.TrackedDownload.DownloadItem.Removed ||
                !trackedDownload.DownloadItem.CanBeRemoved)
            {
                return Task.CompletedTask;
            }

            var downloadClient = _downloadClientProvider.Get(message.TrackedDownload.DownloadClient);
            var definition = downloadClient.Definition as DownloadClientDefinition;

            if (!definition.RemoveFailedDownloads)
            {
                return Task.CompletedTask;
            }

            RemoveFromDownloadClient(trackedDownload, downloadClient);
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(DownloadCompletedEvent message)
        {
            var trackedDownload = message.TrackedDownload;
            var downloadClient = _downloadClientProvider.Get(trackedDownload.DownloadClient);
            var definition = downloadClient.Definition as DownloadClientDefinition;

            MarkItemAsImported(trackedDownload, downloadClient);

            if (trackedDownload.DownloadItem.Removed ||
                !trackedDownload.DownloadItem.CanBeRemoved ||
                trackedDownload.DownloadItem.Status == DownloadItemStatus.Downloading)
            {
                return Task.CompletedTask;
            }

            if (definition?.RemoveCompletedDownloads != true)
            {
                return Task.CompletedTask;
            }

            RemoveFromDownloadClient(message.TrackedDownload, downloadClient);
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(DownloadCanBeRemovedEvent message)
        {
            var trackedDownload = message.TrackedDownload;
            var downloadClient = _downloadClientProvider.Get(trackedDownload.DownloadClient);
            var definition = downloadClient.Definition as DownloadClientDefinition;

            if (trackedDownload.DownloadItem.Removed ||
                !trackedDownload.DownloadItem.CanBeRemoved ||
                definition?.RemoveCompletedDownloads != true)
            {
                return Task.CompletedTask;
            }

            RemoveFromDownloadClient(message.TrackedDownload, downloadClient);
            
            return Task.CompletedTask;
        }

        private void RemoveFromDownloadClient(TrackedDownload trackedDownload, IDownloadClient downloadClient)
        {
            try
            {
                _logger.LogDebug("[{Title}] Removing download from {Name} history", trackedDownload.DownloadItem.Title, trackedDownload.DownloadItem.DownloadClientInfo.Name);
                downloadClient.RemoveItem(trackedDownload.DownloadItem, true);
                trackedDownload.DownloadItem.Removed = true;
            }
            catch (NotSupportedException)
            {
                _logger.LogWarning("Removing item not supported by your download client ({Name}).", downloadClient.Definition.Name);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Couldn't remove item {Title} from client {Name}", trackedDownload.DownloadItem.Title, downloadClient.Name);
            }
        }

        private void MarkItemAsImported(TrackedDownload trackedDownload, IDownloadClient downloadClient)
        {
            try
            {
                _logger.LogDebug("[{Title}] Marking download as imported from {Name}", trackedDownload.DownloadItem.Title, trackedDownload.DownloadItem.DownloadClientInfo.Name);
                downloadClient.MarkItemAsImported(trackedDownload.DownloadItem);
            }
            catch (NotSupportedException e)
            {
                _logger.LogDebug("{Message}", e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Couldn't mark item {Title} as imported from client {Name}", trackedDownload.DownloadItem.Title, downloadClient.Name);
            }
        }
    }
}
