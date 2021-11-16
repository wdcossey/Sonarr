using System;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider.Status;

namespace NzbDrone.Core.Download
{
    public interface IDownloadClientStatusService : IProviderStatusServiceBase<DownloadClientStatus>
    {

    }

    public class DownloadClientStatusService : ProviderStatusServiceBase<IDownloadClient, DownloadClientStatus>, IDownloadClientStatusService
    {
        public DownloadClientStatusService(IDownloadClientStatusRepository providerStatusRepository, IEventAggregator eventAggregator, IRuntimeInfo runtimeInfo, ILogger<DownloadClientStatusService> logger)
            : base(providerStatusRepository, eventAggregator, runtimeInfo, logger)
        {
            MinimumTimeSinceInitialFailure = TimeSpan.FromMinutes(5);
            MaximumEscalationLevel = 5;
        }
    }
}
