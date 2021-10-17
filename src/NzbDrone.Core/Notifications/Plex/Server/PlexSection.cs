﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.Notifications.Plex.Server
{
    public class PlexSectionLocation
    {
        public int Id { get; set; }
        public string Path { get; set; }
    }

    public class PlexSection
    {
        public PlexSection()
        {
            Locations = new List<PlexSectionLocation>();
        }

        [JsonPropertyName("key")]
        public int Id { get; set; }

        public string Type { get; set; }
        public string Language { get; set; }

        [JsonPropertyName("Location")]
        public List<PlexSectionLocation> Locations { get; set; }
    }

    public class PlexSectionsContainer
    {
        public PlexSectionsContainer()
        {
            Sections = new List<PlexSection>();
        }

        [JsonPropertyName("Directory")]
        public List<PlexSection> Sections { get; set; }
    }

    public class PlexSectionLegacy
    {
        [JsonPropertyName("key")]
        public int Id { get; set; }

        public string Type { get; set; }
        public string Language { get; set; }

        [JsonPropertyName("_children")]
        public List<PlexSectionLocation> Locations { get; set; }
    }

    public class PlexMediaContainerLegacy
    {
        [JsonPropertyName("_children")]
        public List<PlexSectionLegacy> Sections { get; set; }
    }
}
