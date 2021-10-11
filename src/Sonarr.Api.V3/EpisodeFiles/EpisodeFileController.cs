using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Tv;
using Sonarr.Http.Extensions;
using BadRequestException = Sonarr.Http.REST.BadRequestException;

namespace Sonarr.Api.V3.EpisodeFiles
{
    [SonarrApiRoute("episodeFile", RouteVersion.V3)]
    //TODO: Remove `SonarrControllerBase<>`
    public class EpisodeFileController :
        SonarrControllerBase<EpisodeFileResource, EpisodeFile>,
        IHandle<EpisodeFileAddedEvent>,
        IHandle<EpisodeFileDeletedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly ISeriesService _seriesService;
        private readonly IUpgradableSpecification _upgradableSpecification;

        public EpisodeFileController(/*IBroadcastSignalRMessage signalRBroadcaster,*/ //TODO: Add SignalR
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

        public void Handle(EpisodeFileAddedEvent message)
        {
            //BroadcastResourceChange(ModelAction.Updated, message.EpisodeFile.Id);
        }

        public void Handle(EpisodeFileDeletedEvent message)
        {
            //BroadcastResourceChange(ModelAction.Deleted, message.EpisodeFile.Id);
        }

        /*[HttpGet("{seriesId:int?}")]
        public new IActionResult GetAllAsync([FromQuery] int? seriesId = null, [FromQuery] IList<int> episodeFileIds = null)
            => Ok(GetEpisodeFiles(seriesId, episodeFileIds));*/

        protected override Task<IList<EpisodeFileResource>> GetAllResourcesAsync()
            => Task.FromResult<IList<EpisodeFileResource>>(GetEpisodeFiles());

        protected override Task<EpisodeFileResource> GetResourceByIdAsync(int id)
            => Task.FromResult(GetEpisodeFile(id));

        protected override Task DeleteResourceByIdAsync(int id)
        {
            DeleteEpisodeFile(id);
            return Task.CompletedTask;
        }

        protected override Task<EpisodeFileResource> UpdateResourceAsync(EpisodeFileResource resource)
        {
            SetQuality(resource);
            return Task.FromResult(GetEpisodeFile(resource.Id));
        }

        protected override Task<EpisodeFileResource> CreateResourceAsync(EpisodeFileResource resource)
            => throw new NotImplementedException();

        [HttpPut("editor")]
        public virtual Task<IActionResult> EditAllAsync()
            => Task.FromResult<IActionResult>(Accepted(SetQuality()));

        [HttpPut("bulk")]
        public virtual Task<IActionResult> DeleteAllAsync()
        {
            DeleteEpisodeFiles();
            return Task.FromResult<IActionResult>(Ok());
        }

        private EpisodeFileResource GetEpisodeFile(int id)
        {
            var episodeFile = _mediaFileService.Get(id);
            var series = _seriesService.GetSeries(episodeFile.SeriesId);

            return episodeFile.ToResource(series, _upgradableSpecification);
        }

        private List<EpisodeFileResource> GetEpisodeFiles(/*int? seriesId = null, IList<int> episodeFileIds = null*/)
        {
            int? seriesId = null;
            if (Request.Query.TryGetValue("seriesId", out var seriesIdQuery))
                seriesId = int.Parse(seriesIdQuery);

            IList<int> episodeFileIds = null;
            if (Request.Query.TryGetValue("episodeFileIds", out var episodeFileIdsQuery))
                episodeFileIds = episodeFileIdsQuery.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => Convert.ToInt32(e))
                    .ToList();

            //var seriesIdQuery = Request.Query.SeriesId;
            //var episodeFileIdsQuery = Request.Query.EpisodeFileIds;

            if (!seriesId.HasValue && episodeFileIds?.Any() != true)
            {
                throw new BadRequestException("seriesId or episodeFileIds must be provided");
            }

            if (seriesId.HasValue)
            {
                //int seriesId = Convert.ToInt32(seriesIdQuery.Value);
                var series = _seriesService.GetSeries(seriesId.Value);
                return _mediaFileService.GetFilesBySeries(seriesId.Value).ConvertAll(f => f.ToResource(series, _upgradableSpecification));
            }

            else
            {
                /*string episodeFileIdsValue = episodeFileIdsQuery.Value.ToString();

                var episodeFileIds = episodeFileIdsValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(e => Convert.ToInt32(e))
                                                        .ToList();*/

                var episodeFiles = _mediaFileService.Get(episodeFileIds);

                return episodeFiles.GroupBy(e => e.SeriesId)
                                   .SelectMany(f => f.ToList()
                                                     .ConvertAll( e => e.ToResource(_seriesService.GetSeries(f.Key), _upgradableSpecification)))
                                   .ToList();
            }
        }

        private void SetQuality(EpisodeFileResource episodeFileResource)
        {
            var episodeFile = _mediaFileService.Get(episodeFileResource.Id);
            episodeFile.Quality = episodeFileResource.Quality;

            if (episodeFileResource.SceneName != null && SceneChecker.IsSceneTitle(episodeFileResource.SceneName))
            {
                episodeFile.SceneName = episodeFileResource.SceneName;
            }

            if (episodeFileResource.ReleaseGroup != null)
            {
                episodeFile.ReleaseGroup = episodeFileResource.ReleaseGroup;
            }

            _mediaFileService.Update(episodeFile);
        }

        private object SetQuality()
        {
            var resource = Request.Body.FromJson<EpisodeFileListResource>();
            var episodeFiles = _mediaFileService.GetFiles(resource.EpisodeFileIds);

            foreach (var episodeFile in episodeFiles)
            {
                if (resource.Language != null)
                {
                    episodeFile.Language = resource.Language;
                }

                if (resource.Quality != null)
                {
                    episodeFile.Quality = resource.Quality;
                }

                if (resource.SceneName != null && SceneChecker.IsSceneTitle(resource.SceneName))
                {
                    episodeFile.SceneName = resource.SceneName;
                }

                if (resource.ReleaseGroup != null)
                {
                    episodeFile.ReleaseGroup = resource.ReleaseGroup;
                }
            }

            _mediaFileService.Update(episodeFiles);

            var series = _seriesService.GetSeries(episodeFiles.First().SeriesId);

            return episodeFiles.ConvertAll(f => f.ToResource(series, _upgradableSpecification));
                //ResponseWithCode(episodeFiles.ConvertAll(f => f.ToResource(series, _upgradableSpecification)), HttpStatusCode.Accepted);
        }

        private void DeleteEpisodeFile(int id)
        {
            var episodeFile = _mediaFileService.Get(id);

            if (episodeFile == null)
            {
                throw new NzbDroneClientException(global::System.Net.HttpStatusCode.NotFound, "Episode file not found");
            }

            var series = _seriesService.GetSeries(episodeFile.SeriesId);

            _mediaFileDeletionService.DeleteEpisodeFile(series, episodeFile);
        }

        private object DeleteEpisodeFiles()
        {
            var resource = Request.Body.FromJson<EpisodeFileListResource>();
            var episodeFiles = _mediaFileService.GetFiles(resource.EpisodeFileIds);
            var series = _seriesService.GetSeries(episodeFiles.First().SeriesId);

            foreach (var episodeFile in episodeFiles)
            {
                _mediaFileDeletionService.DeleteEpisodeFile(series, episodeFile);
            }

            return new object();
        }
    }
}
