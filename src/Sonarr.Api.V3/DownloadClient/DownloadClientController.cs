using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Download;

namespace Sonarr.Api.V3.DownloadClient
{
    [Route("/api/v3/downloadclient")]
    public class DownloadClientController : ProviderControllerBase<DownloadClientResource, IDownloadClient, DownloadClientDefinition>
    {
        public static readonly DownloadClientResourceMapper ResourceMapper = new DownloadClientResourceMapper();

        public DownloadClientController(IDownloadClientFactory downloadClientFactory)
            : base(downloadClientFactory, /*"downloadclient",*/ ResourceMapper)
        {
        }

        protected override void Validate(DownloadClientDefinition definition, bool includeWarnings)
        {
            if (!definition.Enable) return;
            base.Validate(definition, includeWarnings);
        }
    }
}