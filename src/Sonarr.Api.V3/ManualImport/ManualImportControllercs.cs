using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles.EpisodeImport.Manual;
using NzbDrone.Core.Qualities;
using Sonarr.Api.V3.Episodes;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.ManualImport
{
    [ApiController]
    [SonarrApiRoute("manualimport", RouteVersion.V3)]
    public class ManualImportController : ControllerBase
    {
        private readonly IManualImportService _manualImportService;

        public ManualImportController(IManualImportService manualImportService)
            => _manualImportService = manualImportService;

        [HttpGet]
        public IActionResult GetMediaFiles(
            [FromQuery] string folder,
            [FromQuery] string downloadId,
            [FromQuery] bool filterExistingFiles = true,
            [FromQuery] int? seriesId = null)
        {
            var value = _manualImportService.GetMediaFiles(folder, downloadId, seriesId, filterExistingFiles)
                .ToResource().Select(AddQualityWeight);
            return Ok(value);
        }

        [HttpPost]
        public IActionResult ReprocessItems([FromBody] List<ManualImportReprocessResource> items)
        {
            foreach (var item in items)
            {
                var processedItem = _manualImportService.ReprocessItem(item.Path, item.DownloadId, item.SeriesId, item.SeasonNumber, item.EpisodeIds ?? new List<int>(), item.ReleaseGroup, item.Quality, item.Language);

                item.SeasonNumber = processedItem.SeasonNumber;
                item.Episodes = processedItem.Episodes.ToResource();
                item.Rejections = processedItem.Rejections;

                // Only set the language/quality if they're unknown
                if (item.Language == Language.Unknown)
                {
                    item.Language = processedItem.Language;
                }

                if (item.Quality?.Quality == Quality.Unknown)
                {
                    item.Quality = processedItem.Quality;
                }

                if (item.ReleaseGroup.IsNotNullOrWhiteSpace())
                {
                    item.ReleaseGroup = processedItem.ReleaseGroup;
                }

                // Clear episode IDs in favour of the full episode
                item.EpisodeIds = null;
            }

            return Ok(items);
        }

        private static ManualImportResource AddQualityWeight(ManualImportResource item)
        {
            if (item.Quality != null)
            {
                item.QualityWeight = Quality.DefaultQualityDefinitions.Single(q => q.Quality == item.Quality.Quality).Weight;
                item.QualityWeight += item.Quality.Revision.Real * 10;
                item.QualityWeight += item.Quality.Revision.Version;
            }

            return item;
        }
    }
}
