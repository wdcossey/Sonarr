using Microsoft.Extensions.Logging;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.TorrentRss
{
    public class TorrentRssIndexer : HttpIndexerBase<TorrentRssIndexerSettings>
    {
        public override string Name => "Torrent RSS Feed";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override bool SupportsSearch => false;
        public override int PageSize => 0;

        private readonly ITorrentRssParserFactory _torrentRssParserFactory;
        
        public TorrentRssIndexer(ITorrentRssParserFactory torrentRssParserFactory, IHttpClient<TorrentRssIndexer> httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, ILoggerFactory loggerFactory)
            : base(httpClient, indexerStatusService, configService, parsingService, loggerFactory)
        {
            _torrentRssParserFactory = torrentRssParserFactory;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentRssIndexerRequestGenerator { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return _torrentRssParserFactory.GetParser(Settings);
        }
    }
}
