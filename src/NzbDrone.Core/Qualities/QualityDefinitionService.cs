﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;
using System;
using System.Threading.Tasks;
using NzbDrone.Common.Cache;

namespace NzbDrone.Core.Qualities
{
    public interface IQualityDefinitionService
    {
        QualityDefinition Update(QualityDefinition qualityDefinition);
        void UpdateMany(List<QualityDefinition> qualityDefinitions);
        List<QualityDefinition> All();
        QualityDefinition GetById(int id);
        QualityDefinition Get(Quality quality);
    }

    public class QualityDefinitionService : IQualityDefinitionService, IHandleAsync<ApplicationStartedEvent>
    {
        private readonly IQualityDefinitionRepository _repo;
        private readonly ICached<Dictionary<Quality, QualityDefinition>> _cache;
        private readonly ILogger<QualityDefinitionService> _logger;

        public QualityDefinitionService(IQualityDefinitionRepository repo, ICacheManager cacheManager, ILogger<QualityDefinitionService> logger)
        {
            _repo = repo;
            _cache = cacheManager.GetCache<Dictionary<Quality, QualityDefinition>>(this.GetType());
            _logger = logger;
        }

        private Dictionary<Quality, QualityDefinition> GetAll()
        {
            return _cache.Get("all", () => _repo.All().Select(WithWeight).ToDictionary(v => v.Quality), TimeSpan.FromSeconds(5.0));
        }

        public QualityDefinition Update(QualityDefinition qualityDefinition)
        {
            var result = _repo.Update(qualityDefinition);

            _cache.Clear();

            return result;
        }

        public void UpdateMany(List<QualityDefinition> qualityDefinitions)
        {
            _repo.UpdateMany(qualityDefinitions);
        }

        public List<QualityDefinition> All()
        {
            return GetAll().Values.OrderBy(d => d.Weight).ToList();
        }

        public QualityDefinition GetById(int id)
        {
            return GetAll().Values.Single(v => v.Id == id);
        }

        public QualityDefinition Get(Quality quality)
        {
            return GetAll()[quality];
        }

        private void InsertMissingDefinitions()
        {
            List<QualityDefinition> insertList = new List<QualityDefinition>();
            List<QualityDefinition> updateList = new List<QualityDefinition>();

            var allDefinitions = Quality.DefaultQualityDefinitions.OrderBy(d => d.Weight).ToList();
            var existingDefinitions = _repo.All().ToList();

            foreach (var definition in allDefinitions)
            {
                var existing = existingDefinitions.SingleOrDefault(d => d.Quality == definition.Quality);

                if (existing == null)
                {
                    insertList.Add(definition);
                }

                else
                {
                    updateList.Add(existing);
                    existingDefinitions.Remove(existing);
                }
            }

            _repo.InsertMany(insertList);
            _repo.UpdateMany(updateList);
            _repo.DeleteMany(existingDefinitions);

            _cache.Clear();
        }

        private static QualityDefinition WithWeight(QualityDefinition definition)
        {
            definition.Weight = Quality.DefaultQualityDefinitions.Single(d => d.Quality == definition.Quality).Weight;

            return definition;
        }

        public Task HandleAsync(ApplicationStartedEvent message)
        {
            _logger.LogDebug("Setting up default quality config");

            InsertMissingDefinitions();
            
            return Task.CompletedTask;
        }
    }
}
