using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Tv;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.EpisodeFiles
{
    [SonarrApiRoute("episodeFile", RouteVersion.V3)]
    public class EpisodeFileController : ControllerBase,
        IHandle<EpisodeFileAddedEvent>,
        IHandle<EpisodeFileDeletedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly ISeriesService _seriesService;
        private readonly IUpgradableSpecification _upgradableSpecification;

        public EpisodeFileController(/*IBroadcastSignalRMessage signalRBroadcaster,*/ //TODO: SignalR
                             IMediaFileService mediaFileService,
                             IDeleteMediaFiles mediaFileDeletionService,
                             ISeriesService seriesService,
                             IUpgradableSpecification upgradableSpecification)
            //: base(signalRBroadcaster)
        {
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _seriesService = seriesService;
            _upgradableSpecification = upgradableSpecification;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public void Handle(EpisodeFileAddedEvent message)
        {
            //BroadcastResourceChange(ModelAction.Updated, message.EpisodeFile.Id); //TODO: SignalR
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public void Handle(EpisodeFileDeletedEvent message)
        {
            //BroadcastResourceChange(ModelAction.Deleted, message.EpisodeFile.Id); //TODO: SignalR
        }

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

        [HttpGet("{id:int:required}")]
        public IActionResult GetEpisodeFile(int id)
        {
            var episodeFile = _mediaFileService.Get(id);
            var series = _seriesService.GetSeries(episodeFile.SeriesId);
            return Ok(episodeFile.ToResource(series, _upgradableSpecification));
        }

        [HttpDelete("{id:int:required}")]
        public IActionResult DeleteEpisodeFile(int id)
        {
            var episodeFile = _mediaFileService.Get(id);

            if (episodeFile == null)
            {
                //TODO: return NotFound()?!?
                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Episode file not found");
            }

            var series = _seriesService.GetSeries(episodeFile.SeriesId);

            _mediaFileDeletionService.DeleteEpisodeFile(series, episodeFile);

            return Ok(new object());
        }

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

        [HttpPut("editor")]
        public IActionResult EditAllAsync([FromBody] EpisodeFileListResource resource)
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

        [HttpPut("bulk")]
        public IActionResult DeleteAllAsync([FromBody] EpisodeFileListResource resource)
        {
            var episodeFiles = _mediaFileService.GetFiles(resource.EpisodeFileIds);
            var series = _seriesService.GetSeries(episodeFiles.First().SeriesId);

            foreach (var episodeFile in episodeFiles)
                _mediaFileDeletionService.DeleteEpisodeFile(series, episodeFile);

            return Ok(new object());
        }
    }
}
