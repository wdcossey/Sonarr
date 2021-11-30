using System.Text.Json.Serialization;

namespace NzbDrone.Core.Download.Clients.Sabnzbd
{
    public class SabnzbdFullStatus
    {
        // Added in Sabnzbd 2.0.0, my_home was previously in &mode=queue.
        // This is the already resolved completedir path.
        [JsonPropertyName("completedir")]
        public string CompleteDir { get; set; }
    }
}
