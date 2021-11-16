using Microsoft.Extensions.Logging;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.IPTorrents
{
    public class IPTorrents : HttpIndexerBase<IPTorrentsSettings>
    {
        private readonly ILoggerFactory _loggerFactory;
        public override string Name => "IP Torrents";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override bool SupportsSearch => false;
        public override int PageSize => 0;

        public IPTorrents(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, ILoggerFactory loggerFactory)
            : base(httpClient, indexerStatusService, configService, parsingService, loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new IPTorrentsRequestGenerator() { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentRssParser(_loggerFactory) { ParseSizeInDescription = true };
        }
    }
}