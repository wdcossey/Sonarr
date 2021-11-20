using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Extras.Subtitles;
using NzbDrone.Core.MediaFiles.EpisodeImport.Aggregation;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Extras.Metadata
{
    public class ExistingMetadataImporter : ImportExistingExtraFilesBase<MetadataFile>
    {
        private readonly IExtraFileService<MetadataFile> _metadataFileService;
        private readonly IAggregationService _aggregationService;
        private readonly ILogger<ExistingMetadataImporter> _logger;
        private readonly List<IMetadata> _consumers;

        public ExistingMetadataImporter(IExtraFileService<MetadataFile> metadataFileService,
                                        IEnumerable<IMetadata> consumers,
                                        IAggregationService aggregationService,
                                        ILogger<ExistingMetadataImporter> logger)
        : base(metadataFileService)
        {
            _metadataFileService = metadataFileService;
            _aggregationService = aggregationService;
            _logger = logger;
            _consumers = consumers.ToList();
        }

        public override int Order => 0;

        public override IEnumerable<ExtraFile> ProcessFiles(Series series, List<string> filesOnDisk, List<string> importedFiles)
        {
            _logger.LogDebug("Looking for existing metadata in {Path}", series.Path);

            var metadataFiles = new List<MetadataFile>();
            var filterResult = FilterAndClean(series, filesOnDisk, importedFiles);

            foreach (var possibleMetadataFile in filterResult.FilesOnDisk)
            {
                // Don't process files that have known Subtitle file extensions (saves a bit of unecessary processing)

                if (SubtitleFileExtensions.Extensions.Contains(Path.GetExtension(possibleMetadataFile)))
                {
                    continue;
                }

                foreach (var consumer in _consumers)
                {
                    var metadata = consumer.FindMetadataFile(series, possibleMetadataFile);

                    if (metadata == null)
                    {
                        continue;
                    }

                    if (metadata.Type == MetadataType.EpisodeImage ||
                        metadata.Type == MetadataType.EpisodeMetadata)
                    {
                        var localEpisode = new LocalEpisode
                        {
                            FileEpisodeInfo = Parser.Parser.ParsePath(possibleMetadataFile),
                            Series = series,
                            Path = possibleMetadataFile
                        };

                        try
                        {
                            _aggregationService.Augment(localEpisode, null);
                        }
                        catch (AugmentingFailedException)
                        {
                            _logger.LogDebug("Unable to parse extra file: {PossibleMetadataFile}", possibleMetadataFile);
                            continue;
                        }

                        if (localEpisode.Episodes.Empty())
                        {
                            _logger.LogDebug("Cannot find related episodes for: {PossibleMetadataFile}", possibleMetadataFile);
                            continue;
                        }

                        if (localEpisode.Episodes.DistinctBy(e => e.EpisodeFileId).Count() > 1)
                        {
                            _logger.LogDebug("Extra file: {PossibleMetadataFile} does not match existing files.", possibleMetadataFile);
                            continue;
                        }

                        metadata.SeasonNumber = localEpisode.SeasonNumber;
                        metadata.EpisodeFileId = localEpisode.Episodes.First().EpisodeFileId;
                    }

                    metadata.Extension = Path.GetExtension(possibleMetadataFile);

                    metadataFiles.Add(metadata);
                }
            }

            _logger.LogInformation("Found {Count} existing metadata files", metadataFiles.Count);
            _metadataFileService.Upsert(metadataFiles);

            // Return files that were just imported along with files that were
            // previously imported so previously imported files aren't imported twice

            return metadataFiles.Concat(filterResult.PreviouslyImported);
        }
    }
}
