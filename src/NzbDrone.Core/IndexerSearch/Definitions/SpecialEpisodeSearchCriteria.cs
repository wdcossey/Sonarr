using System.Linq;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class SpecialEpisodeSearchCriteria : SearchCriteriaBase
    {
        public string[] EpisodeQueryTitles { get; set; }

        public override string ToString()
        {
            var episodeTitles = EpisodeQueryTitles.ToList();

            return episodeTitles.Count > 0
                ? $"[{Series.Title}] Specials"
                : $"[{Series.Title} : {string.Join(",", EpisodeQueryTitles)}]";
        }
    }
}
