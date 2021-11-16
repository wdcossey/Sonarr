using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download
{
    public interface IDownloadClientFactory : IProviderFactory<IDownloadClient, DownloadClientDefinition>
    {
        List<IDownloadClient> DownloadHandlingEnabled(bool filterBlockedClients = true);
    }

    public class DownloadClientFactory : ProviderFactory<IDownloadClient, DownloadClientDefinition>, IDownloadClientFactory
    {
        private readonly IDownloadClientStatusService _downloadClientStatusService;
        private readonly ILogger<DownloadClientFactory> _logger;

        public DownloadClientFactory(IDownloadClientStatusService downloadClientStatusService,
                                     IDownloadClientRepository providerRepository,
                                     IEnumerable<IDownloadClient> providers,
                                     IServiceProvider serviceProvider,
                                     IEventAggregator eventAggregator,
                                     ILogger<DownloadClientFactory> logger)
            : base(providerRepository, providers, serviceProvider, eventAggregator, logger)
        {
            _downloadClientStatusService = downloadClientStatusService;
            _logger = logger;
        }

        protected override List<DownloadClientDefinition> Active()
        {
            return base.Active().Where(c => c.Enable).ToList();
        }

        public override void SetProviderCharacteristics(IDownloadClient provider, DownloadClientDefinition definition)
        {
            base.SetProviderCharacteristics(provider, definition);

            definition.Protocol = provider.Protocol;
        }

        public List<IDownloadClient> DownloadHandlingEnabled(bool filterBlockedClients = true)
        {
            var enabledClients = GetAvailableProviders();

            if (filterBlockedClients)
            {
                return FilterBlockedClients(enabledClients).ToList();
            }

            return enabledClients.ToList();
        }

        private IEnumerable<IDownloadClient> FilterBlockedClients(IEnumerable<IDownloadClient> clients)
        {
            var blockedIndexers = _downloadClientStatusService.GetBlockedProviders().ToDictionary(v => v.ProviderId, v => v);

            foreach (var client in clients)
            {
                DownloadClientStatus downloadClientStatus;
                if (blockedIndexers.TryGetValue(client.Definition.Id, out downloadClientStatus))
                {
                    _logger.LogDebug("Temporarily ignoring download client {Name} till {LocalTime} due to recent failures.", client.Definition.Name, downloadClientStatus.DisabledTill.Value.ToLocalTime());
                    continue;
                }

                yield return client;
            }
        }

        public override ValidationResult Test(DownloadClientDefinition definition)
        {
            var result = base.Test(definition);

            if ((result == null || result.IsValid) && definition.Id != 0)
            {
                _downloadClientStatusService.RecordSuccess(definition.Id);
            }

            return result;
        }
    }
}
