using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.Notifications.Plex.Server
{
    public class PlexSectionItemGuid
    {
        public string Id { get; set; }
    }

    public class PlexSectionItem
    {
        public PlexSectionItem()
        {
            Guids = new List<PlexSectionItemGuid>();
        }

        [JsonPropertyName("ratingKey")]
        public string Id { get; set; }

        public string Title { get; set; }

        public int Year { get; set; }

        [JsonPropertyName("Guid")]
        public List<PlexSectionItemGuid> Guids { get; set; }
    }

    public class PlexSectionResponse
    {
        [JsonPropertyName("Metadata")]
        public List<PlexSectionItem> Items { get; set; }
    }

    public class PlexSectionResponseLegacy
    {
        [JsonPropertyName("_children")]
        public List<PlexSectionItem> Items { get; set; }
    }
}
