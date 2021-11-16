using System.Numerics;
using System.Text.Json.Serialization;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Download.Clients.QBittorrent
{
    // torrent properties from the list returned by /query/torrents
    public class QBittorrentTorrent
    {
        public string Hash { get; set; } // Torrent hash

        public string Name { get; set; } // Torrent name

        public long Size { get; set; } // Torrent size (bytes)

        public double Progress { get; set; } // Torrent progress (%/100)

        [JsonConverter(typeof(JsonBigIntegerConverter))]
        public BigInteger Eta { get; set; } // Torrent ETA (seconds) (QBit contains a bug exceeding ulong limits)

        public string State { get; set; } // Torrent state. See possible values here below

        public string Label { get; set; } // Label of the torrent
        public string Category { get; set; } // Category of the torrent (3.3.5+)

        [JsonPropertyName("save_path")]
        public string SavePath { get; set; } // Torrent save path

        [JsonPropertyName("content_path")]
        public string ContentPath { get; set; } // Torrent save path

        public float Ratio { get; set; } // Torrent share ratio

        [JsonPropertyName("ratio_limit")] // Per torrent seeding ratio limit (-2 = use global, -1 = unlimited)
        public float RatioLimit { get; set; } = -2;

        [JsonPropertyName("seeding_time")]
        public long? SeedingTime { get; set; } // Torrent seeding time (not provided by the list api)

        [JsonPropertyName("seeding_time_limit")] // Per torrent seeding time limit (-2 = use global, -1 = unlimited)
        public long SeedingTimeLimit { get; set; } = -2;
    }

    public class QBittorrentTorrentProperties
    {
        public string Hash { get; set; } // Torrent hash

        [JsonPropertyName("save_path")]
        public string SavePath { get; set; }

        [JsonPropertyName("seeding_time")]
        public long SeedingTime { get; set; } // Torrent seeding time
    }

    public class QBittorrentTorrentFile
    {
        public string Name { get; set; }
    }
}
