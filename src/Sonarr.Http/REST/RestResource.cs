
using System.Text.Json.Serialization;

namespace Sonarr.Http.REST
{
    public abstract class RestResource
    {

        //[JsonPropertyName(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Id { get; set; }

        [JsonIgnore]
        public virtual string ResourceName => GetType().Name.ToLowerInvariant().Replace("resource", "");
    }
}