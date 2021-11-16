﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Extras
{
    public class ExistingExtraFileService : IHandle<SeriesScannedEvent>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskScanService _diskScanService;
        private readonly List<IImportExistingExtraFiles> _existingExtraFileImporters;
        private readonly ILogger<ExistingExtraFileService> _logger;

        public ExistingExtraFileService(IDiskProvider diskProvider,
                                        IDiskScanService diskScanService,
                                        IEnumerable<IImportExistingExtraFiles> existingExtraFileImporters,
                                        ILogger<ExistingExtraFileService> logger)
        {
            _diskProvider = diskProvider;
            _diskScanService = diskScanService;
            _existingExtraFileImporters = existingExtraFileImporters.OrderBy(e => e.Order).ToList();
            _logger = logger;
        }

        public void Handle(SeriesScannedEvent message)
        {
            var series = message.Series;
            var extraFiles = new List<ExtraFile>();

            if (!_diskProvider.FolderExists(series.Path))
            {
                return;
            }

            _logger.LogDebug("Looking for existing extra files in {Path}", series.Path);

            var filesOnDisk = _diskScanService.GetNonVideoFiles(series.Path);
            var possibleExtraFiles = _diskScanService.FilterPaths(series.Path, filesOnDisk);

            var filteredFiles = possibleExtraFiles;
            var importedFiles = new List<string>();

            foreach (var existingExtraFileImporter in _existingExtraFileImporters)
            {
                var imported = existingExtraFileImporter.ProcessFiles(series, filteredFiles, importedFiles);

                importedFiles.AddRange(imported.Select(f => Path.Combine(series.Path, f.RelativePath)));
            }

            _logger.LogInformation("Found {Count} extra files", extraFiles.Count);
        }
    }
}
