using System.Collections.Generic;

namespace NzbDrone.Core.Profiles.Releases
{
    public class PreferredWordMatchResults
    {
        public List<string> All
        {
            get;
            set;
        }

        public Dictionary<string, List<string>> ByReleaseProfile
        {
            get;
            set;
        }

        public PreferredWordMatchResults()
        {
            All = new List<string>();
            ByReleaseProfile = new Dictionary<string, List<string>>();
        }
    }
}
