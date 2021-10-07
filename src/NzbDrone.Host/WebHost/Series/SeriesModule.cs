using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NzbDrone.Api.Series;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Profiles.Languages;
using NzbDrone.Core.SeriesStats;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.SignalR;

namespace NzbDrone.Host.WebHost.Series
{
    public class SeriesModule : WebApiController
    {
        private readonly ISeriesService _seriesService;
        private readonly IAddSeriesService _addSeriesService;
        private readonly ISeriesStatisticsService _seriesStatisticsService;
        private readonly ISceneMappingService _sceneMappingService;
        private readonly IMapCoversToLocal _coverMapper;
        private readonly ILanguageProfileService _languageProfileService;

        public SeriesModule(
            IBroadcastSignalRMessage signalRBroadcaster,
            ISeriesService seriesService,
            IAddSeriesService addSeriesService,
            ISeriesStatisticsService seriesStatisticsService,
            ISceneMappingService sceneMappingService,
            IMapCoversToLocal coverMapper,
            ILanguageProfileService languageProfileService,
            RootFolderValidator rootFolderValidator,
            SeriesPathValidator seriesPathValidator,
            SeriesExistsValidator seriesExistsValidator,
            SeriesAncestorValidator seriesAncestorValidator,
            SystemFolderValidator systemFolderValidator,
            ProfileExistsValidator profileExistsValidator,
            LanguageProfileExistsValidator languageProfileExistsValidator)
        {
            _seriesService = seriesService;
            _addSeriesService = addSeriesService;
            _seriesStatisticsService = seriesStatisticsService;
            _sceneMappingService = sceneMappingService;

            _coverMapper = coverMapper;
            _languageProfileService = languageProfileService;

        }

        [Route(HttpVerbs.Get, "/")]
        public Task<IList<SeriesResource>> GetStatusAsync()
        {
            var includeSeasonImages = false; //Request.QueryString["includeSeasonImages"];
            var seriesStats = _seriesStatisticsService.SeriesStatistics();
            var seriesResources = _seriesService.GetAllSeries().Select(s => s.ToResource(includeSeasonImages)).ToList();

            MapCoversToLocal(seriesResources.ToArray());
            LinkSeriesStatistics(seriesResources, seriesStats);
            PopulateAlternateTitles(seriesResources);

            return Task.FromResult<IList<SeriesResource>>(seriesResources);
        }


        private void MapCoversToLocal(params SeriesResource[] series)
        {
            foreach (var seriesResource in series)
            {
                _coverMapper.ConvertToLocalUrls(seriesResource.Id, seriesResource.Images);
            }
        }

        private void LinkSeriesStatistics(List<SeriesResource> resources, List<SeriesStatistics> seriesStatistics)
        {
            var dictSeriesStats = seriesStatistics.ToDictionary(v => v.SeriesId);

            foreach (var series in resources)
            {
                var stats = dictSeriesStats.GetValueOrDefault(series.Id);
                if (stats == null) continue;

                LinkSeriesStatistics(series, stats);
            }
        }

        private void LinkSeriesStatistics(SeriesResource resource, SeriesStatistics seriesStatistics)
        {
            resource.TotalEpisodeCount = seriesStatistics.TotalEpisodeCount;
            resource.EpisodeCount = seriesStatistics.EpisodeCount;
            resource.EpisodeFileCount = seriesStatistics.EpisodeFileCount;
            resource.NextAiring = seriesStatistics.NextAiring;
            resource.PreviousAiring = seriesStatistics.PreviousAiring;
            resource.SizeOnDisk = seriesStatistics.SizeOnDisk;

            if (seriesStatistics.SeasonStatistics != null)
            {
                var dictSeasonStats = seriesStatistics.SeasonStatistics.ToDictionary(v => v.SeasonNumber);

                foreach (var season in resource.Seasons)
                {
                    season.Statistics = dictSeasonStats.GetValueOrDefault(season.SeasonNumber).ToResource();
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
    }
}
