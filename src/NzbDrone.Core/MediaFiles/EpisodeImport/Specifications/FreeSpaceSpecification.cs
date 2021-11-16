using System;
using System.IO;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Specifications
{
    public class FreeSpaceSpecification : IImportDecisionEngineSpecification
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly ILogger<FreeSpaceSpecification> _logger;

        public FreeSpaceSpecification(IDiskProvider diskProvider, IConfigService configService, ILogger<FreeSpaceSpecification> logger)
        {
            _diskProvider = diskProvider;
            _configService = configService;
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalEpisode localEpisode, DownloadClientItem downloadClientItem)
        {
            if (_configService.SkipFreeSpaceCheckWhenImporting)
            {
                _logger.LogDebug("Skipping free space check when importing");
                return Decision.Accept();
            }

            try
            {
                if (localEpisode.ExistingFile)
                {
                    _logger.LogDebug("Skipping free space check for existing episode");
                    return Decision.Accept();
                }

                var path = Directory.GetParent(localEpisode.Series.Path);
                var freeSpace = _diskProvider.GetAvailableSpace(path.FullName);

                if (!freeSpace.HasValue)
                {
                    _logger.LogDebug("Free space check returned an invalid result for: {Path}", path);
                    return Decision.Accept();
                }

                if (freeSpace < localEpisode.Size + _configService.MinimumFreeSpaceWhenImporting.Megabytes())
                {
                    _logger.LogWarning("Not enough free space ({FreeSpace}) to import: {LocalEpisode} ({Size})", freeSpace, localEpisode, localEpisode.Size);
                    return Decision.Reject("Not enough free space");
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "Unable to check free disk space while importing.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to check free disk space while importing. {Path}", localEpisode.Path);
            }

            return Decision.Accept();
        }
    }
}
