using System.Text.Json.Serialization;

namespace Sonarr.Blazor.Shared
{
    public class RestResource
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonIgnore]
        public virtual string ResourceName => GetType().Name.ToLowerInvariant().Replace("resource", "");
    }
}
