using NzbDrone.Core.Download;

namespace Sonarr.Api.V3.DownloadClient
{
    [SonarrApiRoute("downloadclient", RouteVersion.V3)]
    public class DownloadClientController : ProviderControllerBase<DownloadClientResource, IDownloadClient, DownloadClientDefinition>
    {
        public static readonly DownloadClientResourceMapper ResourceMapper = new();

        public DownloadClientController(IDownloadClientFactory downloadClientFactory)
            : base(downloadClientFactory, ResourceMapper) { }

        protected override void Validate(DownloadClientDefinition definition, bool includeWarnings)
        {
            if (!definition.Enable) return;
            base.Validate(definition, includeWarnings);
        }
    }
}