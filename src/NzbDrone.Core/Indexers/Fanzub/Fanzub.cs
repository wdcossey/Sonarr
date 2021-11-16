using Microsoft.Extensions.Logging;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Fanzub
{
    public class Fanzub : HttpIndexerBase<FanzubSettings>
    {
        private readonly ILoggerFactory _loggerFactory;
        public override string Name => "Fanzub";

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

        public Fanzub(IHttpClient<Fanzub> httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, ILoggerFactory loggerFactory)
            : base(httpClient, indexerStatusService, configService, parsingService, loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new FanzubRequestGenerator() { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new RssParser(_loggerFactory) { UseEnclosureUrl = true, UseEnclosureLength = true };
        }
    }
}
