using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Indexers.Rarbg
{
    public class RarbgResponse
    {
        public string error { get; set; }
        public int? error_code { get; set; }
        public List<RarbgTorrent> torrent_results { get; set; }
    }

    public class RarbgTorrent
    {
        public string title { get; set; }
        public string category { get; set; }
        public string download { get; set; }
        public int? seeders { get; set; }
        public int? leechers { get; set; }
        public long size { get; set; }

        [JsonConverter(typeof(JsonDateTimeConverter))]
        public DateTime pubdate { get; set; }
        public RarbgTorrentInfo episode_info { get; set; }
        public int? ranked { get; set; }
        public string info_page { get; set; }
    }

    public class RarbgTorrentInfo
    {
        public string imdb { get; set; }
        public int? tvrage { get; set; }
        public int? tvdb { get; set; }
    }
}
