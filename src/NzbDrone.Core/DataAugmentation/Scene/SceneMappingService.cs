﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.DataAugmentation.Scene
{
    public interface ISceneMappingService
    {
        List<string> GetSceneNames(int tvdbId, List<int> seasonNumbers, List<int> sceneSeasonNumbers);
        int? FindTvdbId(string sceneTitle, string releaseTitle, int sceneSeasonNumber);
        List<SceneMapping> FindByTvdbId(int tvdbId);
        SceneMapping FindSceneMapping(string sceneTitle, string releaseTitle, int sceneSeasonNumber);
        int? GetSceneSeasonNumber(string seriesTitle, string releaseTitle);
    }

    public class SceneMappingService : ISceneMappingService,
                                       IHandleAsync<SeriesRefreshStartingEvent>,
                                       IHandleAsync<SeriesAddedEvent>,
                                       IHandleAsync<SeriesImportedEvent>,
                                       IExecuteAsync<UpdateSceneMappingCommand>
    {
        private readonly ISceneMappingRepository _repository;
        private readonly IEnumerable<ISceneMappingProvider> _sceneMappingProviders;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<SceneMappingService> _logger;
        private readonly ICachedDictionary<List<SceneMapping>> _getTvdbIdCache;
        private readonly ICachedDictionary<List<SceneMapping>> _findByTvdbIdCache;
        private bool _updatedAfterStartup;

        public SceneMappingService(ISceneMappingRepository repository,
                                   ICacheManager cacheManager,
                                   IEnumerable<ISceneMappingProvider> sceneMappingProviders,
                                   IEventAggregator eventAggregator,
                                   ILogger<SceneMappingService> logger)
        {
            _repository = repository;
            _sceneMappingProviders = sceneMappingProviders;
            _eventAggregator = eventAggregator;
            _logger = logger;

            _getTvdbIdCache = cacheManager.GetCacheDictionary<List<SceneMapping>>(GetType(), "tvdb_id");
            _findByTvdbIdCache = cacheManager.GetCacheDictionary<List<SceneMapping>>(GetType(), "find_tvdb_id");
        }

        public List<string> GetSceneNames(int tvdbId, List<int> seasonNumbers, List<int> sceneSeasonNumbers)
        {
            var mappings = FindByTvdbId(tvdbId);

            if (mappings == null)
            {
                return new List<string>();
            }

            var names = mappings.Where(n => seasonNumbers.Contains(n.SeasonNumber ?? -1) ||
                                            sceneSeasonNumbers.Contains(n.SceneSeasonNumber ?? -1) ||
                                            (n.SeasonNumber ?? -1) == -1 && (n.SceneSeasonNumber ?? -1) == -1 && n.SceneOrigin != "tvdb")
                                .Where(n => IsEnglish(n.SearchTerm))
                                .Select(n => n.SearchTerm).Distinct().ToList();

            return names;
        }

        public int? FindTvdbId(string seriesTitle, string releaseTitle, int sceneSeasonNumber)
        {
            return FindSceneMapping(seriesTitle, releaseTitle, sceneSeasonNumber)?.TvdbId;
        }

        public List<SceneMapping> FindByTvdbId(int tvdbId)
        {
            if (_findByTvdbIdCache.Count == 0)
            {
                RefreshCache();
            }

            var mappings = _findByTvdbIdCache.Find(tvdbId.ToString());

            if (mappings == null)
            {
                return new List<SceneMapping>();
            }

            return mappings;
        }

        public SceneMapping FindSceneMapping(string seriesTitle, string releaseTitle, int sceneSeasonNumber)
        {
            var mappings = FindMappings(seriesTitle, releaseTitle);

            if (mappings == null)
            {
                return null;
            }

            mappings = FilterSceneMappings(mappings, sceneSeasonNumber);

            var distinctMappings = mappings.DistinctBy(v => v.TvdbId).ToList();

            if (distinctMappings.Count == 0)
            {
                return null;
            }

            if (distinctMappings.Count == 1)
            {
                var mapping = distinctMappings.First();
                _logger.LogDebug("Found scene mapping for: {SeriesTitle}. TVDB ID for mapping: {TvdbId}", seriesTitle, mapping.TvdbId);
                return distinctMappings.First();
            }

            throw new InvalidSceneMappingException(mappings, releaseTitle);
        }

        public int? GetSceneSeasonNumber(string seriesTitle, string releaseTitle)
        {
            return FindSceneMapping(seriesTitle, releaseTitle, -1)?.SceneSeasonNumber;
        }

        private void UpdateMappings()
        {
            _logger.LogInformation("Updating Scene mappings");

            _updatedAfterStartup = true;

            foreach (var sceneMappingProvider in _sceneMappingProviders)
            {
                try
                {
                    var mappings = sceneMappingProvider.GetSceneMappings();

                    if (mappings.Any())
                    {
                        _repository.Clear(sceneMappingProvider.GetType().Name);

                        mappings.RemoveAll(sceneMapping =>
                        {
                            if (sceneMapping.Title.IsNullOrWhiteSpace() ||
                                sceneMapping.SearchTerm.IsNullOrWhiteSpace())
                            {
                                _logger.LogWarning("Invalid scene mapping found for: {TvdbId}, skipping", sceneMapping.TvdbId);
                                return true;
                            }

                            return false;
                        });

                        foreach (var sceneMapping in mappings)
                        {
                            sceneMapping.ParseTerm = sceneMapping.Title.CleanSeriesTitle();
                            sceneMapping.Type = sceneMappingProvider.GetType().Name;
                        }

                        _repository.InsertMany(mappings.ToList());
                    }
                    else
                    {
                        _logger.LogWarning("Received empty list of mapping. will not update");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to Update Scene Mappings");
                }
            }

            RefreshCache();

            _eventAggregator.PublishEvent(new SceneMappingsUpdatedEvent());
        }

        private List<SceneMapping> FindMappings(string seriesTitle, string releaseTitle)
        {
            if (_getTvdbIdCache.Count == 0)
            {
                RefreshCache();
            }

            var candidates = _getTvdbIdCache.Find(seriesTitle.CleanSeriesTitle());

            if (candidates == null)
            {
                return null;
            }

            candidates = FilterSceneMappings(candidates, releaseTitle);

            if (candidates.Count <= 1)
            {
                return candidates;
            }

            var exactMatch = candidates.OrderByDescending(v => v.SeasonNumber)
                                       .Where(v => v.Title == seriesTitle)
                                       .ToList();

            if (exactMatch.Any())
            {
                return exactMatch;
            }

            var closestMatch = candidates.OrderBy(v => seriesTitle.LevenshteinDistance(v.Title, 10, 1, 10))
                                         .ThenByDescending(v => v.SeasonNumber)
                                         .First();


            return candidates.Where(v => v.Title == closestMatch.Title).ToList();
        }

        private void RefreshCache()
        {
            var mappings = _repository.All().ToList();

            _getTvdbIdCache.Update(mappings.GroupBy(v => v.ParseTerm).ToDictionary(v => v.Key, v => v.ToList()));
            _findByTvdbIdCache.Update(mappings.GroupBy(v => v.TvdbId).ToDictionary(v => v.Key.ToString(), v => v.ToList()));
        }

        private List<SceneMapping> FilterSceneMappings(List<SceneMapping> candidates, string releaseTitle)
        {
            var filteredCandidates = candidates.Where(v => v.FilterRegex.IsNotNullOrWhiteSpace()).ToList();
            var normalCandidates = candidates.Except(filteredCandidates).ToList();

            if (releaseTitle.IsNullOrWhiteSpace())
            {
                return normalCandidates;
            }

            var simpleTitle = Parser.Parser.SimplifyTitle(releaseTitle);

            filteredCandidates = filteredCandidates.Where(v => Regex.IsMatch(simpleTitle, v.FilterRegex)).ToList();

            if (filteredCandidates.Any())
            {
                return filteredCandidates;
            }

            return normalCandidates;
        }

        private List<SceneMapping> FilterSceneMappings(List<SceneMapping> candidates, int sceneSeasonNumber)
        {
            var filteredCandidates = candidates.Where(v => (v.SceneSeasonNumber ?? -1) != -1 && (v.SeasonNumber ?? -1) != -1).ToList();
            var normalCandidates = candidates.Except(filteredCandidates).ToList();

            if (sceneSeasonNumber == -1)
            {
                return normalCandidates;
            }

            if (filteredCandidates.Any())
            {
                filteredCandidates = filteredCandidates.Where(v => v.SceneSeasonNumber <= sceneSeasonNumber)
                                                       .GroupBy(v => v.Title)
                                                       .Select(d => d.OrderByDescending(v => v.SceneSeasonNumber)
                                                                     .ThenByDescending(v => v.SeasonNumber)
                                                                     .First())
                                                       .ToList();

                return filteredCandidates;
            }

            return normalCandidates;
        }

        private bool IsEnglish(string title)
        {
            return title.All(c => c <= 255);
        }

        public Task HandleAsync(SeriesRefreshStartingEvent message)
        {
            if (message.ManualTrigger && (_findByTvdbIdCache.IsExpired(TimeSpan.FromMinutes(1)) || !_updatedAfterStartup))
                UpdateMappings();
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(SeriesAddedEvent message)
        {
            if (!_updatedAfterStartup)
                UpdateMappings();
            
            return Task.CompletedTask;
        }

        public Task HandleAsync(SeriesImportedEvent message)
        {
            if (!_updatedAfterStartup)
                UpdateMappings();
            
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(UpdateSceneMappingCommand message)
        {
            UpdateMappings();
            return Task.CompletedTask;
        }
    }
}
