﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.EpisodeImport;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles
{
    public class DownloadedEpisodesCommandService : IExecuteAsync<DownloadedEpisodesScanCommand>
    {
        private readonly IDownloadedEpisodesImportService _downloadedEpisodesImportService;
        private readonly ITrackedDownloadService _trackedDownloadService;
        private readonly IDiskProvider _diskProvider;
        private readonly ICompletedDownloadService _completedDownloadService;
        private readonly ILogger<DownloadedEpisodesCommandService> _logger;

        public DownloadedEpisodesCommandService(IDownloadedEpisodesImportService downloadedEpisodesImportService,
                                                ITrackedDownloadService trackedDownloadService,
                                                IDiskProvider diskProvider,
                                                ICompletedDownloadService completedDownloadService,
                                                ILogger<DownloadedEpisodesCommandService> logger)
        {
            _downloadedEpisodesImportService = downloadedEpisodesImportService;
            _trackedDownloadService = trackedDownloadService;
            _diskProvider = diskProvider;
            _completedDownloadService = completedDownloadService;
            _logger = logger;
        }

        private List<ImportResult> ProcessPath(DownloadedEpisodesScanCommand message)
        {
            if (!_diskProvider.FolderExists(message.Path) && !_diskProvider.FileExists(message.Path))
            {
                _logger.LogWarning("Folder/File specified for import scan [{Path}] doesn't exist.", message.Path);
                return new List<ImportResult>();
            }

            if (message.DownloadClientId.IsNotNullOrWhiteSpace())
            {
                var trackedDownload = _trackedDownloadService.Find(message.DownloadClientId);

                if (trackedDownload != null)
                {
                    _logger.LogDebug("External directory scan request for known download {DownloadClientId}. [{Path}]", message.DownloadClientId, message.Path);

                    var importResults = _downloadedEpisodesImportService.ProcessPath(message.Path, message.ImportMode, trackedDownload.RemoteEpisode.Series, trackedDownload.DownloadItem);

                    _completedDownloadService.VerifyImport(trackedDownload, importResults);

                    return importResults;
                }

                _logger.LogWarning("External directory scan request for unknown download {DownloadClientId}, attempting normal import. [{Path}]", message.DownloadClientId, message.Path);
            }

            return _downloadedEpisodesImportService.ProcessPath(message.Path, message.ImportMode);
        }

        public Task ExecuteAsync(DownloadedEpisodesScanCommand message)
        {
            List<ImportResult> importResults;

            if (message.Path.IsNotNullOrWhiteSpace())
            {
                importResults = ProcessPath(message);
            }
            else
            {
                throw new ArgumentException("A path must be provided", "path");
            }

            if (importResults == null || importResults.All(v => v.Result != ImportResultType.Imported))
            {
                // Atm we don't report it as a command failure, coz that would cause the download to be failed.
                _logger.ProgressDebug("Failed to import");
            }
            
            return Task.CompletedTask;
        }
    }
}
