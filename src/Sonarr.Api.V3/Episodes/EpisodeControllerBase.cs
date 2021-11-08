using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Tv;
using Sonarr.Api.V3.EpisodeFiles;
using Sonarr.Api.V3.Series;


namespace Sonarr.Api.V3.Episodes
{
    public abstract class EpisodeControllerBase : ControllerBase
    {
        protected readonly IEpisodeService _episodeService;
        protected readonly ISeriesService _seriesService;
        protected readonly IUpgradableSpecification _upgradableSpecification;

        protected EpisodeControllerBase(IEpisodeService episodeService,
            ISeriesService seriesService,
            IUpgradableSpecification upgradableSpecification)
        {
            _episodeService = episodeService;
            _seriesService = seriesService;
            _upgradableSpecification = upgradableSpecification;
        }

        [HttpGet("{id:int}")]
        public EpisodeResource GetEpisode(int id)
        {
            var episode = _episodeService.GetEpisode(id);
            var resource = MapToResource(episode, true, true, true);
            return resource;
        }

        protected EpisodeResource MapToResource(Episode episode, bool includeSeries, bool includeEpisodeFile, bool includeImages)
        {
            var resource = episode.ToResource();

            if (includeSeries || includeEpisodeFile || includeImages)
            {
                var series = episode.Series ?? _seriesService.GetSeries(episode.SeriesId);

                if (includeSeries)
                    resource.Series = series.ToResource();

                if (includeEpisodeFile && episode.EpisodeFileId != 0)
                    resource.EpisodeFile = episode.EpisodeFile.Value.ToResource(series, _upgradableSpecification);

                if (includeImages)
                    resource.Images = episode.Images;
            }

            return resource;
        }

        protected List<EpisodeResource> MapToResource(List<Episode> episodes, bool includeSeries, bool includeEpisodeFile, bool includeImages)
        {
            var result = episodes.ToResource();

            if (includeSeries || includeEpisodeFile || includeImages)
            {
                var seriesDict = new Dictionary<int, NzbDrone.Core.Tv.Series>();
                for (var i = 0; i < episodes.Count; i++)
                {
                    var episode = episodes[i];
                    var resource = result[i];

                    var series = episode.Series ?? seriesDict.GetValueOrDefault(episodes[i].SeriesId) ?? _seriesService.GetSeries(episodes[i].SeriesId);
                    seriesDict[series.Id] = series;

                    if (includeSeries)
                    {
                        resource.Series = series.ToResource();
                    }

                    if (includeEpisodeFile && episode.EpisodeFileId != 0)
                    {
                        resource.EpisodeFile = episode.EpisodeFile.Value.ToResource(series, _upgradableSpecification);
                    }

                    if (includeImages)
                    {
                        resource.Images = episode.Images;
                    }
                }
            }

            return result;
        }
    }
}
