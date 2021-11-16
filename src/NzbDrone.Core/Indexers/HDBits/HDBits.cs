using Microsoft.Extensions.Logging;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.HDBits
{
    public class HDBits : HttpIndexerBase<HDBitsSettings>
    {
        public override string Name => "HDBits";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override int PageSize => 30;

        public HDBits(IHttpClient<HDBits> httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, ILoggerFactory loggerFactory)
            : base(httpClient, indexerStatusService, configService, parsingService, loggerFactory)
        { }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new HDBitsRequestGenerator() { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new HDBitsParser(Settings);
        }
    }
}
