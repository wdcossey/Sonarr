using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Tv
{
    public interface IEpisodeMonitoredService
    {
        void SetEpisodeMonitoredStatus(Series series, MonitoringOptions monitoringOptions);
    }

    public class EpisodeMonitoredService : IEpisodeMonitoredService
    {
        private readonly ISeriesService _seriesService;
        private readonly IEpisodeService _episodeService;
        private readonly ILogger<EpisodeMonitoredService> _logger;

        public EpisodeMonitoredService(ISeriesService seriesService, IEpisodeService episodeService, ILogger<EpisodeMonitoredService> logger)
        {
            _seriesService = seriesService;
            _episodeService = episodeService;
            _logger = logger;
        }

        public void SetEpisodeMonitoredStatus(Series series, MonitoringOptions monitoringOptions)
        {
            // Update the series without changing the episodes
            if (monitoringOptions == null)
            {
                _seriesService.UpdateSeries(series, false);
                return;
            }

            // Fallback for v2 endpoints
            if (monitoringOptions.Monitor == MonitorTypes.Unknown)
            {
                LegacySetEpisodeMonitoredStatus(series, monitoringOptions);
                return;
            }

            var firstSeason = series.Seasons.Select(s => s.SeasonNumber).Where(s => s > 0).MinOrDefault();
            var lastSeason = series.Seasons.Select(s => s.SeasonNumber).MaxOrDefault();
            var episodes = _episodeService.GetEpisodeBySeries(series.Id);

            switch (monitoringOptions.Monitor)
            {
                case MonitorTypes.All:
                    _logger.LogDebug("[{Title}] Monitoring all episodes", series.Title);
                    ToggleEpisodesMonitoredState(episodes, e => e.SeasonNumber > 0);

                    break;

                case MonitorTypes.Future:
                    _logger.LogDebug("[{Title}] Monitoring future episodes", series.Title);
                    ToggleEpisodesMonitoredState(episodes, e => e.SeasonNumber > 0 && (!e.AirDateUtc.HasValue || e.AirDateUtc >= DateTime.UtcNow));

                    break;

                case MonitorTypes.Missing:
                    _logger.LogDebug("[{Title}] Monitoring missing episodes", series.Title);
                    ToggleEpisodesMonitoredState(episodes, e => e.SeasonNumber > 0 && !e.HasFile);

                    break;

                case MonitorTypes.Existing:
                    _logger.LogDebug("[{Title}] Monitoring existing episodes", series.Title);
                    ToggleEpisodesMonitoredState(episodes, e => e.SeasonNumber > 0 && e.HasFile);

                    break;

                case MonitorTypes.Pilot:
                    _logger.LogDebug("[{Title}] Monitoring first episode episodes", series.Title);
                    ToggleEpisodesMonitoredState(episodes,
                        e => e.SeasonNumber > 0 && e.SeasonNumber == firstSeason && e.EpisodeNumber == 1);

                    break;

                case MonitorTypes.FirstSeason:
                    _logger.LogDebug("[{Title}] Monitoring first season episodes", series.Title);
                    ToggleEpisodesMonitoredState(episodes, e => e.SeasonNumber > 0 && e.SeasonNumber == firstSeason);

                    break;

                case MonitorTypes.LatestSeason:
                    if (episodes.Where(e => e.SeasonNumber == lastSeason)
                                .All(e => e.AirDateUtc.HasValue &&
                                          e.AirDateUtc.Value.Before(DateTime.UtcNow) &&
                                          !e.AirDateUtc.Value.InLastDays(90)))
                    {
                        _logger.LogDebug("[{Title}] Unmonitoring all episodes because latest season aired more than 90 days ago", series.Title);
                        ToggleEpisodesMonitoredState(episodes, e => false);
                        break;
                    }

                    _logger.LogDebug("[{Title}] Monitoring latest season episodes", series.Title);

                    ToggleEpisodesMonitoredState(episodes, e => e.SeasonNumber > 0 && e.SeasonNumber == lastSeason);

                    break;

                case MonitorTypes.None:
                    _logger.LogDebug("[{Title}] Unmonitoring all episodes", series.Title);
                    ToggleEpisodesMonitoredState(episodes, e => false);

                    break;
            }

            var monitoredSeasons = episodes.Where(e => e.Monitored)
                                           .Select(e => e.SeasonNumber)
                                           .Distinct()
                                           .ToList();

            foreach (var season in series.Seasons)
            {
                var seasonNumber = season.SeasonNumber;

                // Monitor the season when:
                // - Not specials
                // - The latest season
                // - Not only supposed to monitor the first season
                if (seasonNumber > 0 &&
                    seasonNumber == lastSeason &&
                    monitoringOptions.Monitor != MonitorTypes.FirstSeason &&
                    monitoringOptions.Monitor != MonitorTypes.Pilot &&
                    monitoringOptions.Monitor != MonitorTypes.None)
                {
                    season.Monitored = true;
                }
                // Don't monitor season 1 if only the pilot episode is monitored
                else if (seasonNumber == firstSeason && monitoringOptions.Monitor == MonitorTypes.Pilot)
                {
                    season.Monitored = false;
                }
                // Monitor the season if it has any monitor episodes
                else if (monitoredSeasons.Contains(seasonNumber))
                {
                    season.Monitored = true;
                }
                // Don't monitor the season
                else
                {
                    season.Monitored = false;
                }
            }

            _episodeService.UpdateEpisodes(episodes);
            _seriesService.UpdateSeries(series, false);
        }

        private void LegacySetEpisodeMonitoredStatus(Series series, MonitoringOptions monitoringOptions)
        {
            _logger.LogDebug("[{Title}] Setting episode monitored status.", series.Title);

            var episodes = _episodeService.GetEpisodeBySeries(series.Id);

            if (monitoringOptions.IgnoreEpisodesWithFiles)
            {
                _logger.LogDebug("Unmonitoring Episodes with Files");
                ToggleEpisodesMonitoredState(episodes.Where(e => e.HasFile), false);
            }
            else
            {
                _logger.LogDebug("Monitoring Episodes with Files");
                ToggleEpisodesMonitoredState(episodes.Where(e => e.HasFile), true);
            }

            if (monitoringOptions.IgnoreEpisodesWithoutFiles)
            {
                _logger.LogDebug("Unmonitoring Episodes without Files");
                ToggleEpisodesMonitoredState(episodes.Where(e => !e.HasFile && e.AirDateUtc.HasValue && e.AirDateUtc.Value.Before(DateTime.UtcNow)), false);
            }
            else
            {
                _logger.LogDebug("Monitoring Episodes without Files");
                ToggleEpisodesMonitoredState(episodes.Where(e => !e.HasFile && e.AirDateUtc.HasValue && e.AirDateUtc.Value.Before(DateTime.UtcNow)), true);
            }

            var lastSeason = series.Seasons.Select(s => s.SeasonNumber).MaxOrDefault();

            foreach (var s in series.Seasons)
            {
                var season = s;

                // If the season is unmonitored we should unmonitor all episodes in that season

                if (!season.Monitored)
                {
                    _logger.LogDebug("Unmonitoring all episodes in season {SeasonNumber}", season.SeasonNumber);
                    ToggleEpisodesMonitoredState(episodes.Where(e => e.SeasonNumber == season.SeasonNumber), false);
                }

                // If the season is not the latest season and all it's episodes are unmonitored the season will be unmonitored

                if (season.SeasonNumber < lastSeason)
                {
                    if (episodes.Where(e => e.SeasonNumber == season.SeasonNumber).All(e => !e.Monitored))
                    {
                        _logger.LogDebug("Unmonitoring season {SeasonNumber} because all episodes are not monitored", season.SeasonNumber);
                        season.Monitored = false;
                    }
                }
            }

            _episodeService.UpdateEpisodes(episodes);

            _seriesService.UpdateSeries(series, false);
        }

        private void ToggleEpisodesMonitoredState(IEnumerable<Episode> episodes, bool monitored)
        {
            foreach (var episode in episodes)
            {
                episode.Monitored = monitored;
            }
        }

        private void ToggleEpisodesMonitoredState(List<Episode> episodes, Func<Episode, bool> predicate)
        {
            ToggleEpisodesMonitoredState(episodes.Where(predicate), true);
            ToggleEpisodesMonitoredState(episodes.Where(e => !predicate(e)), false);

        }
    }
}
