﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Cache;
using NzbDrone.Core.DataAugmentation.Scene;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Tv;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.DataAugmentation.Xem
{
    public class XemService : ISceneMappingProvider, IHandle<SeriesUpdatedEvent>, IHandle<SeriesRefreshStartingEvent>
    {
        private readonly IEpisodeService _episodeService;
        private readonly IXemProxy _xemProxy;
        private readonly ISeriesService _seriesService;
        private readonly ILogger<XemService> _logger;
        private readonly ICachedDictionary<bool> _cache;

        public XemService(IEpisodeService episodeService,
                           IXemProxy xemProxy,
                           ISeriesService seriesService,
                           ICacheManager cacheManager,
                           ILogger<XemService> logger)
        {
            _episodeService = episodeService;
            _xemProxy = xemProxy;
            _seriesService = seriesService;
            _logger = logger;
            _cache = cacheManager.GetCacheDictionary<bool>(GetType(), "mappedTvdbid");
        }

        private void PerformUpdate(Series series)
        {
            _logger.LogDebug("Updating scene numbering mapping for: {Series}", series);

            try
            {
                var mappings = _xemProxy.GetSceneTvdbMappings(series.TvdbId);

                if (!mappings.Any() && !series.UseSceneNumbering)
                {
                    _logger.LogDebug("Mappings for: {Series} are empty, skipping", series);
                    return;
                }

                var episodes = _episodeService.GetEpisodeBySeries(series.Id);

                foreach (var episode in episodes)
                {
                    episode.SceneAbsoluteEpisodeNumber = null;
                    episode.SceneSeasonNumber = null;
                    episode.SceneEpisodeNumber = null;
                    episode.UnverifiedSceneNumbering = false;
                }

                foreach (var mapping in mappings)
                {
                    _logger.LogDebug("Setting scene numbering mappings for {Series} S{Season:00}E{Episode:00}", series, mapping.Tvdb.Season, mapping.Tvdb.Episode);

                    var episode = episodes.SingleOrDefault(e => e.SeasonNumber == mapping.Tvdb.Season && e.EpisodeNumber == mapping.Tvdb.Episode);

                    if (episode == null)
                    {
                        _logger.LogDebug("Information hasn't been added to TheTVDB yet, skipping");
                        continue;
                    }

                    if (mapping.Scene.Absolute == 0 &&
                        mapping.Scene.Season == 0 &&
                        mapping.Scene.Episode == 0)
                    {
                        _logger.LogDebug("Mapping for {} S{Season:00}E{Episode:00} is invalid, skipping", series, mapping.Tvdb.Season, mapping.Tvdb.Episode);
                        continue;
                    }

                    episode.SceneAbsoluteEpisodeNumber = mapping.Scene.Absolute;
                    episode.SceneSeasonNumber = mapping.Scene.Season;
                    episode.SceneEpisodeNumber = mapping.Scene.Episode;
                }

                if (episodes.Any(v => v.SceneEpisodeNumber.HasValue && v.SceneSeasonNumber != 0))
                {
                    ExtrapolateMappings(series, episodes, mappings);
                }

                _episodeService.UpdateEpisodes(episodes);
                series.UseSceneNumbering = mappings.Any();
                _seriesService.UpdateSeries(series);

                _logger.LogDebug("XEM mapping updated for {Series}", series);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating scene numbering mappings for {Series}", series);
            }
        }

        private void ExtrapolateMappings(Series series, List<Episode> episodes, List<Model.XemSceneTvdbMapping> mappings)
        {
            var mappedEpisodes = episodes.Where(v => v.SeasonNumber != 0 && v.SceneEpisodeNumber.HasValue).ToList();
            var mappedSeasons = new HashSet<int>(mappedEpisodes.Select(v => v.SeasonNumber).Distinct());

            var sceneEpisodeMappings = mappings.ToLookup(v => v.Scene.Season)
                                               .ToDictionary(v => v.Key, e => new HashSet<int>(e.Select(v => v.Scene.Episode)));

            var firstTvdbEpisodeBySeason = mappings.ToLookup(v => v.Tvdb.Season)
                                                   .ToDictionary(v => v.Key, e => e.Min(v => v.Tvdb.Episode));

            var lastSceneSeason = mappings.Select(v => v.Scene.Season).Max();
            var lastTvdbSeason = mappings.Select(v => v.Tvdb.Season).Max();

            // Mark all episodes not on the xem as unverified.
            foreach (var episode in episodes)
            {
                if (episode.SeasonNumber == 0) continue;
                if (episode.SceneEpisodeNumber.HasValue) continue;

                if (mappedSeasons.Contains(episode.SeasonNumber))
                {
                    // Mark if a mapping exists for an earlier episode in this season.
                    if (firstTvdbEpisodeBySeason[episode.SeasonNumber] <= episode.EpisodeNumber)
                    {
                        episode.UnverifiedSceneNumbering = true;
                        continue;
                    }

                    // Mark if a mapping exists with a scene number to this episode.
                    if (sceneEpisodeMappings.ContainsKey(episode.SeasonNumber) &&
                        sceneEpisodeMappings[episode.SeasonNumber].Contains(episode.EpisodeNumber))
                    {
                        episode.UnverifiedSceneNumbering = true;
                        continue;
                    }
                }
                else if (lastSceneSeason != lastTvdbSeason && episode.SeasonNumber > lastTvdbSeason)
                {
                    episode.UnverifiedSceneNumbering = true;
                }
            }

            foreach (var episode in episodes)
            {
                if (episode.SeasonNumber == 0) continue;
                if (episode.SceneEpisodeNumber.HasValue) continue;
                if (episode.SeasonNumber < lastTvdbSeason) continue;
                if (!episode.UnverifiedSceneNumbering) continue;

                var seasonMappings = mappings.Where(v => v.Tvdb.Season == episode.SeasonNumber).ToList();
                if (seasonMappings.Any(v => v.Tvdb.Episode >= episode.EpisodeNumber))
                {
                    continue;
                }

                if (seasonMappings.Any())
                {
                    var lastEpisodeMapping = seasonMappings.OrderBy(v => v.Tvdb.Episode).Last();
                    var lastSceneSeasonMapping = mappings.Where(v => v.Scene.Season == lastEpisodeMapping.Scene.Season).OrderBy(v => v.Scene.Episode).Last();

                    if (lastSceneSeasonMapping.Tvdb.Season == 0)
                    {
                        continue;
                    }

                    var offset = episode.EpisodeNumber - lastEpisodeMapping.Tvdb.Episode;

                    episode.SceneSeasonNumber = lastEpisodeMapping.Scene.Season;
                    episode.SceneEpisodeNumber = lastEpisodeMapping.Scene.Episode + offset;
                    episode.SceneAbsoluteEpisodeNumber = lastEpisodeMapping.Scene.Absolute + offset;
                }
                else if (lastTvdbSeason != lastSceneSeason)
                {
                    var offset = episode.SeasonNumber - lastTvdbSeason;

                    episode.SceneSeasonNumber = lastSceneSeason + offset;
                    episode.SceneEpisodeNumber = episode.EpisodeNumber;
                    // TODO: SceneAbsoluteEpisodeNumber.
                }
            }
        }

        private void UpdateXemSeriesIds()
        {
            try
            {
                var ids = _xemProxy.GetXemSeriesIds();

                if (ids.Any())
                {
                    _cache.Update(ids.ToDictionary(v => v.ToString(), v => true));
                    return;
                }

                _cache.ExtendTTL();
                _logger.LogWarning("Failed to update Xem series list.");
            }
            catch (Exception ex)
            {
                _cache.ExtendTTL();
                _logger.LogWarning(ex, "Failed to update Xem series list.");
            }
        }

        public List<SceneMapping> GetSceneMappings()
        {
            var mappings = _xemProxy.GetSceneTvdbNames();

            return mappings;
        }

        public void Handle(SeriesUpdatedEvent message)
        {
            if (_cache.IsExpired(TimeSpan.FromHours(3)))
            {
                UpdateXemSeriesIds();
            }

            if (_cache.Count == 0)
            {
                _logger.LogDebug("Scene numbering is not available");
                return;
            }

            if (!_cache.Find(message.Series.TvdbId.ToString()) && !message.Series.UseSceneNumbering)
            {
                _logger.LogDebug("Scene numbering is not available for {Title} [{TvdbId}]", message.Series.Title, message.Series.TvdbId);
                return;
            }

            PerformUpdate(message.Series);
        }

        public void Handle(SeriesRefreshStartingEvent message)
        {
            if (message.ManualTrigger && _cache.IsExpired(TimeSpan.FromMinutes(1)))
                UpdateXemSeriesIds();
        }
    }
}
