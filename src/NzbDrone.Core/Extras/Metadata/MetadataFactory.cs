﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata
{
    public interface IMetadataFactory : IProviderFactory<IMetadata, MetadataDefinition>
    {
        List<IMetadata> Enabled();
    }

    public class MetadataFactory : ProviderFactory<IMetadata, MetadataDefinition>, IMetadataFactory
    {
        private readonly IMetadataRepository _providerRepository;

        public MetadataFactory(IMetadataRepository providerRepository, IEnumerable<IMetadata> providers, IServiceProvider serviceProvider, IEventAggregator eventAggregator, ILogger<MetadataFactory> logger)
            : base(providerRepository, providers, serviceProvider, eventAggregator, logger)
        {
            _providerRepository = providerRepository;
        }

        protected override void InitializeProviders()
        {
            var definitions = new List<MetadataDefinition>();

            foreach (var provider in _providers)
            {
                definitions.Add(new MetadataDefinition
                {
                    Enable = false,
                    Name = provider.Name,
                    Implementation = provider.GetType().Name,
                    Settings = (IProviderConfig)Activator.CreateInstance(provider.ConfigContract)
                });
            }

            var currentProviders = All();

            var newProviders = definitions.Where(def => currentProviders.All(c => c.Implementation != def.Implementation)).ToList();

            if (newProviders.Any())
            {
                _providerRepository.InsertMany(newProviders.Cast<MetadataDefinition>().ToList());
            }
        }

        public List<IMetadata> Enabled()
        {
            return GetAvailableProviders().Where(n => ((MetadataDefinition)n.Definition).Enable).ToList();
        }
    }
}