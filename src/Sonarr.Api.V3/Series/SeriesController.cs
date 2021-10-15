using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.SeriesStats;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Commands;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Series
{
    [ApiController]
    [SonarrApiRoute("series", RouteVersion.V3)]
    public class SeriesController : ControllerBase
    {
        private readonly ISeriesService _seriesService;
        private readonly IAddSeriesService _addSeriesService;
        private readonly ISeriesStatisticsService _seriesStatisticsService;
        private readonly ISceneMappingService _sceneMappingService;
        private readonly IMapCoversToLocal _coverMapper;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IRootFolderService _rootFolderService;

        public SeriesController(/*IBroadcastSignalRMessage signalRBroadcaster,*/
            ISeriesService seriesService,
            IAddSeriesService addSeriesService,
            ISeriesStatisticsService seriesStatisticsService,
            ISceneMappingService sceneMappingService,
            IMapCoversToLocal coverMapper,
            IManageCommandQueue commandQueueManager,
            IRootFolderService rootFolderService,
            RootFolderValidator rootFolderValidator,
            MappedNetworkDriveValidator mappedNetworkDriveValidator,
            SeriesPathValidator seriesPathValidator,
            SeriesExistsValidator seriesExistsValidator,
            SeriesAncestorValidator seriesAncestorValidator,
            SystemFolderValidator systemFolderValidator,
            ProfileExistsValidator profileExistsValidator,
            LanguageProfileExistsValidator languageProfileExistsValidator,
            SeriesFolderAsRootFolderValidator seriesFolderAsRootFolderValidator)
        {
            _seriesService = seriesService;
            _addSeriesService = addSeriesService;
            _seriesStatisticsService = seriesStatisticsService;
            _sceneMappingService = sceneMappingService;

            _coverMapper = coverMapper;
            _commandQueueManager = commandQueueManager;
            _rootFolderService = rootFolderService;

            /*GetResourceAll = AllSeries;
            GetResourceById = GetSeries;
            CreateResource = AddSeries;
            UpdateResource = UpdateSeries;
            DeleteResource = DeleteSeries;

            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.QualityProfileId));

            SharedValidator.RuleFor(s => s.Path)
                           .Cascade(CascadeMode.StopOnFirstFailure)
                           .IsValidPath()
                           .SetValidator(rootFolderValidator)
                           .SetValidator(mappedNetworkDriveValidator)
                           .SetValidator(seriesPathValidator)
                           .SetValidator(seriesAncestorValidator)
                           .SetValidator(systemFolderValidator)
                           .When(s => !s.Path.IsNullOrWhiteSpace());

            SharedValidator.RuleFor(s => s.QualityProfileId).SetValidator(profileExistsValidator);
            SharedValidator.RuleFor(s => s.LanguageProfileId).SetValidator(languageProfileExistsValidator);

            PostValidator.RuleFor(s => s.Path).IsValidPath().When(s => s.RootFolderPath.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.RootFolderPath)
                         .IsValidPath()
                         .SetValidator(seriesFolderAsRootFolderValidator)
                         .When(s => s.Path.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.Title).NotEmpty();
            PostValidator.RuleFor(s => s.TvdbId).GreaterThan(0).SetValidator(seriesExistsValidator);

            PutValidator.RuleFor(s => s.Path).IsValidPath();*/
        }


        [HttpGet]
        public IActionResult AllSeries([FromQuery] int tvdbId = 0, [FromQuery] bool includeSeasonImages = false)
        {
            //var tvdbId = Request.GetIntegerQueryParameter("tvdbId");
            //var includeSeasonImages = Request.GetBooleanQueryParameter("includeSeasonImages");
            var seriesStats = _seriesStatisticsService.SeriesStatistics();
            var seriesResources = new List<SeriesResource>();

            if (tvdbId > 0)
            {
                seriesResources.AddIfNotNull(_seriesService.FindByTvdbId(tvdbId).ToResource(includeSeasonImages));
            }
            else
            {
                seriesResources.AddRange(_seriesService.GetAllSeries().Select(s => s.ToResource(includeSeasonImages)));
            }

            MapCoversToLocal(seriesResources.ToArray());
            LinkSeriesStatistics(seriesResources, seriesStats);
            PopulateAlternateTitles(seriesResources);
            seriesResources.ForEach(LinkRootFolderPath);

            return Ok(seriesResources);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetSeries(int id, [FromQuery] bool includeSeasonImages = false)
        {
            var series = _seriesService.GetSeries(id);
            return Ok(GetSeriesResource(series, includeSeasonImages));
        }

        [HttpPost]
        public IActionResult AddSeries([FromBody] SeriesResource seriesResource)
        {
            var series = _addSeriesService.AddSeries(seriesResource.ToModel());
            return Created($"{Request.Path}/{series.Id}", GetSeriesResource(series, false));
        }

        [HttpPut]
        [HttpPut("{id:int?}")] //Needed for routing, not much else!
        public IActionResult UpdateSeries(int id, [FromBody] SeriesResource seriesResource, [FromQuery] bool moveFiles = false)
        {
            var series = _seriesService.GetSeries(seriesResource.Id);

            if (moveFiles)
            {
                var sourcePath = series.Path;
                var destinationPath = seriesResource.Path;

                _commandQueueManager.Push(new MoveSeriesCommand
                {
                    SeriesId = series.Id,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath,
                    Trigger = CommandTrigger.Manual
                });
            }

            var updateSeries = _seriesService.UpdateSeries(seriesResource.ToModel(series));

            //BroadcastResourceChange(ModelAction.Updated, seriesResource);

            return Accepted(GetSeriesResource(updateSeries, false));
        }

        [HttpDelete]
        public IActionResult DeleteSeries(int id, [FromQuery] bool deleteFiles = false, [FromQuery] bool addImportListExclusion = false)
        {
            _seriesService.DeleteSeries(id, deleteFiles, addImportListExclusion);
            return Ok(new object());
        }

        private SeriesResource GetSeriesResource(NzbDrone.Core.Tv.Series series, bool includeSeasonImages)
        {
            if (series == null) return null;

            var resource = series.ToResource(includeSeasonImages);
            MapCoversToLocal(resource);
            FetchAndLinkSeriesStatistics(resource);
            PopulateAlternateTitles(resource);
            LinkRootFolderPath(resource);

            return resource;
        }

        private void MapCoversToLocal(params SeriesResource[] series)
        {
            foreach (var seriesResource in series)
            {
                _coverMapper.ConvertToLocalUrls(seriesResource.Id, seriesResource.Images);
            }
        }

        private void FetchAndLinkSeriesStatistics(SeriesResource resource)
        {
            LinkSeriesStatistics(resource, _seriesStatisticsService.SeriesStatistics(resource.Id));
        }

        private void LinkSeriesStatistics(List<SeriesResource> resources, List<SeriesStatistics> seriesStatistics)
        {
            foreach (var series in resources)
            {
                var stats = seriesStatistics.SingleOrDefault(ss => ss.SeriesId == series.Id);
                if (stats == null) continue;

                LinkSeriesStatistics(series, stats);
            }
        }

        private void LinkSeriesStatistics(SeriesResource resource, SeriesStatistics seriesStatistics)
        {
            resource.PreviousAiring = seriesStatistics.PreviousAiring;
            resource.NextAiring = seriesStatistics.NextAiring;
            resource.Statistics = seriesStatistics.ToResource(resource.Seasons);

            if (seriesStatistics.SeasonStatistics != null)
            {
                foreach (var season in resource.Seasons)
                {
                    season.Statistics = seriesStatistics.SeasonStatistics.SingleOrDefault(s => s.SeasonNumber == season.SeasonNumber).ToResource();
                }
            }
        }

        private void PopulateAlternateTitles(List<SeriesResource> resources)
        {
            foreach (var resource in resources)
            {
                PopulateAlternateTitles(resource);
            }
        }

        private void PopulateAlternateTitles(SeriesResource resource)
        {
            var mappings = _sceneMappingService.FindByTvdbId(resource.TvdbId);

            if (mappings == null) return;

            resource.AlternateTitles = mappings.ConvertAll(AlternateTitleResourceMapper.ToResource);
        }

        private void LinkRootFolderPath(SeriesResource resource)
        {
            resource.RootFolderPath = _rootFolderService.GetBestRootFolderPath(resource.Path);
        }
    }
}
