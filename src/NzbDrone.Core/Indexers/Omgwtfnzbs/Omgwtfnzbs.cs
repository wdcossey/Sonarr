using Microsoft.Extensions.Logging;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Omgwtfnzbs
{
    public class Omgwtfnzbs : HttpIndexerBase<OmgwtfnzbsSettings>
    {
        private readonly ILoggerFactory _loggerFactory;
        public override string Name => "omgwtfnzbs";

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

        public Omgwtfnzbs(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, ILoggerFactory loggerFactory)
            : base(httpClient, indexerStatusService, configService, parsingService, loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new OmgwtfnzbsRequestGenerator() { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new OmgwtfnzbsRssParser(_loggerFactory);
        }
    }
}
