using System.Text.Json.Serialization;

namespace NzbDrone.Core.Download.Clients.Transmission
{
    public class TransmissionConfig
    {
        [JsonPropertyName("rpc-version")]
        public string RpcVersion { get; set; }
        public string Version { get; set; }

        [JsonPropertyName("download-dir")]
        public string DownloadDir { get; set; }

        public double SeedRatioLimit { get; set; }
        public bool SeedRatioLimited { get; set; }

        [JsonPropertyName("idle-seeding-limit")]
        public long IdleSeedingLimit { get; set; }
        [JsonPropertyName("idle-seeding-limit-enabled")]
        public bool IdleSeedingLimitEnabled { get; set; }
    }
}
