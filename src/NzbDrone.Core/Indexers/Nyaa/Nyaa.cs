using Microsoft.Extensions.Logging;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Nyaa
{
    public class Nyaa : HttpIndexerBase<NyaaSettings>
    {
        private readonly ILoggerFactory _loggerFactory;
        public override string Name => "Nyaa";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override int PageSize => 100;

        public Nyaa(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, ILoggerFactory loggerFactory)
            : base(httpClient, indexerStatusService, configService, parsingService, loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NyaaRequestGenerator() { Settings = Settings, PageSize = PageSize };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentRssParser(_loggerFactory) { UseGuidInfoUrl = true, ParseSizeInDescription = true, ParseSeedersInDescription = true };
        }
    }
}