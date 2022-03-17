using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Tv;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.EpisodeFiles
{
    [SonarrApiRoute("episodeFile", RouteVersion.V3)]
    public class EpisodeFileController : ControllerBase
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly ISeriesService _seriesService;
        private readonly IUpgradableSpecification _upgradableSpecification;

        public EpisodeFileController(
                IMediaFileService mediaFileService,
                IDeleteMediaFiles mediaFileDeletionService,
                ISeriesService seriesService,
                IUpgradableSpecification upgradableSpecification)
        {
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _seriesService = seriesService;
            _upgradableSpecification = upgradableSpecification;
        }

        [ProducesResponseType(typeof(List<EpisodeFileResource>), StatusCodes.Status200OK)]
        [HttpGet]
        public IActionResult GetEpisodeFiles(
            [FromQuery] int? seriesId = null,
            [FromQuery] IList<int> episodeFileIds = null)
        {
            if (!seriesId.HasValue && episodeFileIds?.Any() != true)
                return BadRequest($"{nameof(seriesId)} or {nameof(episodeFileIds)} must be provided");

            if (seriesId.HasValue)
            {
                var series = _seriesService.GetSeries(seriesId.Value);
                return Ok(_mediaFileService.GetFilesBySeries(seriesId.Value)
                    .ConvertAll(f => f.ToResource(series, _upgradableSpecification)));
            }

            var episodeFiles = _mediaFileService.Get(episodeFileIds);

            return Ok(episodeFiles.GroupBy(e => e.SeriesId)
                .SelectMany(f => f.ToList()
                    .ConvertAll(e => e.ToResource(_seriesService.GetSeries(f.Key), _upgradableSpecification))));
        }

        [ProducesResponseType(typeof(EpisodeFileResource), StatusCodes.Status200OK)]
        [HttpGet("{id:int:required}")]
        public IActionResult GetEpisodeFile(int id)
        {
            var episodeFile = _mediaFileService.Get(id);
            var series = _seriesService.GetSeries(episodeFile.SeriesId);
            return Ok(episodeFile.ToResource(series, _upgradableSpecification));
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpDelete("{id:int:required}")]
        public IActionResult DeleteEpisodeFile(int id)
        {
            var episodeFile = _mediaFileService.Get(id)
                              ?? throw new NzbDroneClientException(HttpStatusCode.NotFound, "Episode file not found");

            var series = _seriesService.GetSeries(episodeFile.SeriesId);

            _mediaFileDeletionService.DeleteEpisodeFile(series, episodeFile);

            return Ok(new object());
        }

        [ProducesResponseType(typeof(EpisodeFileResource), StatusCodes.Status202Accepted)]
        [HttpPut]
        [HttpPut("{id:int?}")]
        public IActionResult SetQuality(int? id, [FromBody] EpisodeFileResource resource)
        {
            var episodeFile = _mediaFileService.Get(resource.Id);
            episodeFile.Quality = resource.Quality;

            if (resource.SceneName != null && SceneChecker.IsSceneTitle(resource.SceneName))
                episodeFile.SceneName = resource.SceneName;

            if (resource.ReleaseGroup != null)
                episodeFile.ReleaseGroup = resource.ReleaseGroup;

            var series = _seriesService.GetSeries(episodeFile.SeriesId);
            return Accepted(_mediaFileService.Update(episodeFile).ToResource(series, _upgradableSpecification));
        }

        [ProducesResponseType(typeof(List<EpisodeFileResource>), StatusCodes.Status202Accepted)]
        [HttpPut("editor")]
        public IActionResult EditAll([FromBody] EpisodeFileListResource resource)
        {
            var episodeFiles = _mediaFileService
                .GetFiles(resource.EpisodeFileIds)
                .Select(episodeFile =>
                {
                    if (resource.Language != null)
                        episodeFile.Language = resource.Language;

                    if (resource.Quality != null)
                        episodeFile.Quality = resource.Quality;

                    if (resource.SceneName != null && SceneChecker.IsSceneTitle(resource.SceneName))
                        episodeFile.SceneName = resource.SceneName;

                    if (resource.ReleaseGroup != null)
                        episodeFile.ReleaseGroup = resource.ReleaseGroup;

                    return episodeFile;
                }).ToList();

            _mediaFileService.Update(episodeFiles);

            var series = _seriesService.GetSeries(episodeFiles.First().SeriesId);

            return Accepted(episodeFiles.ConvertAll(f => f.ToResource(series, _upgradableSpecification)));
        }

        [ProducesResponseType(typeof(List<EpisodeFileResource>), StatusCodes.Status202Accepted)]
        [HttpPut("bulk")]
        public IActionResult SetPropertiesBulk([FromBody] List<EpisodeFileResource> resource)
        {
            var episodeFiles = _mediaFileService.GetFiles(resource.Select(r => r.Id));

            foreach (var episodeFile in episodeFiles)
            {
                var resourceEpisodeFile = resource.Single(r => r.Id == episodeFile.Id);

                if (resourceEpisodeFile.Language != null)
                {
                    episodeFile.Language = resourceEpisodeFile.Language;
                }

                if (resourceEpisodeFile.Quality != null)
                {
                    episodeFile.Quality = resourceEpisodeFile.Quality;
                }

                if (resourceEpisodeFile.SceneName != null && SceneChecker.IsSceneTitle(resourceEpisodeFile.SceneName))
                {
                    episodeFile.SceneName = resourceEpisodeFile.SceneName;
                }

                if (resourceEpisodeFile.ReleaseGroup != null)
                {
                    episodeFile.ReleaseGroup = resourceEpisodeFile.ReleaseGroup;
                }
            }

            _mediaFileService.Update(episodeFiles);

            var series = _seriesService.GetSeries(episodeFiles.First().SeriesId);

            return Accepted(episodeFiles.ConvertAll(f => f.ToResource(series, _upgradableSpecification)));
        }

        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [HttpDelete("bulk")]
        public IActionResult DeleteEpisodeFiles([FromBody] EpisodeFileListResource resource)
        {
            var episodeFiles = _mediaFileService.GetFiles(resource.EpisodeFileIds);
            var series = _seriesService.GetSeries(episodeFiles.First().SeriesId);

            foreach (var episodeFile in episodeFiles)
                _mediaFileDeletionService.DeleteEpisodeFile(series, episodeFile);

            return Ok(new object());
        }
    }
}