using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.Notifications.Plex.Server
{
    public class PlexPreferences
    {
        [JsonPropertyName("Setting")]
        public List<PlexPreference> Preferences { get; set; }
    }

    public class PlexPreference
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class PlexPreferencesLegacy
    {
        [JsonPropertyName("_children")]
        public List<PlexPreference> Preferences { get; set; }
    }
}
