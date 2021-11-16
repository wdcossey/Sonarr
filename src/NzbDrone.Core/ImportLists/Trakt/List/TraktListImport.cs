using Microsoft.Extensions.Logging;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.Trakt.List
{
    public class TraktListImport : TraktImportBase<TraktListSettings>
    {
        public TraktListImport(IImportListRepository netImportRepository,
                               IHttpClient<TraktListImport> httpClient,
                               IImportListStatusService netImportStatusService,
                               IConfigService configService,
                               IParsingService parsingService,
                               ILogger<TraktListImport> logger)
        : base(netImportRepository, httpClient, netImportStatusService, configService, parsingService, logger)
        {
        }

        public override string Name => "Trakt List";

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new TraktListRequestGenerator()
            {
                Settings = Settings,
                ClientId = ClientId
            };
        }
    }
}
