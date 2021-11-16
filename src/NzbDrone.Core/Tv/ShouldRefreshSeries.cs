using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace NzbDrone.Core.Tv
{
    public interface ICheckIfSeriesShouldBeRefreshed
    {
        bool ShouldRefresh(Series series);
    }

    public class ShouldRefreshSeries : ICheckIfSeriesShouldBeRefreshed
    {
        private readonly IEpisodeService _episodeService;
        private readonly ILogger<ShouldRefreshSeries> _logger;

        public ShouldRefreshSeries(IEpisodeService episodeService, ILogger<ShouldRefreshSeries> logger)
        {
            _episodeService = episodeService;
            _logger = logger;
        }

        public bool ShouldRefresh(Series series)
        {
            if (series.LastInfoSync < DateTime.UtcNow.AddDays(-30))
            {
                _logger.LogTrace("Series {Title} last updated more than 30 days ago, should refresh.", series.Title);
                return true;
            }

            if (series.LastInfoSync >= DateTime.UtcNow.AddHours(-6))
            {
                _logger.LogTrace("Series {Title} last updated less than 6 hours ago, should not be refreshed.", series.Title);
                return false;
            }

            if (series.Status != SeriesStatusType.Ended)
            {
                _logger.LogTrace("Series {Title} is not ended, should refresh.", series.Title);
                return true;
            }

            var lastEpisode = _episodeService.GetEpisodeBySeries(series.Id).OrderByDescending(e => e.AirDateUtc).FirstOrDefault();

            if (lastEpisode != null && lastEpisode.AirDateUtc > DateTime.UtcNow.AddDays(-30))
            {
                _logger.LogTrace("Last episode in {Title} aired less than 30 days ago, should refresh.", series.Title);
                return true;
            }

            _logger.LogTrace("Series {Title} ended long ago, should not be refreshed.", series.Title);
            return false;
        }
    }
}
