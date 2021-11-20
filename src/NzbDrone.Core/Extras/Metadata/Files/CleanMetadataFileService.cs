using System.IO;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Extras.Metadata.Files
{
    public interface ICleanMetadataService
    {
        void Clean(Series series);
    }

    public class CleanExtraFileService : ICleanMetadataService
    {
        private readonly IMetadataFileService _metadataFileService;
        private readonly IDiskProvider _diskProvider;
        private readonly ILogger<CleanExtraFileService> _logger;

        public CleanExtraFileService(IMetadataFileService metadataFileService,
                                    IDiskProvider diskProvider,
                                    ILogger<CleanExtraFileService> logger)
        {
            _metadataFileService = metadataFileService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public void Clean(Series series)
        {
            _logger.LogDebug("Cleaning missing metadata files for series: {Title}", series.Title);

            var metadataFiles = _metadataFileService.GetFilesBySeries(series.Id);

            foreach (var metadataFile in metadataFiles)
            {
                if (!_diskProvider.FileExists(Path.Combine(series.Path, metadataFile.RelativePath)))
                {
                    _logger.LogDebug("Deleting metadata file from database: {Title}", metadataFile.RelativePath);
                    _metadataFileService.Delete(metadataFile.Id);
                }
            }
        }
    }
}
