namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class SingleEpisodeSearchCriteria : SearchCriteriaBase
    {
        public int EpisodeNumber { get; set; }
        public int SeasonNumber { get; set; }

        public override string ToString()
        {
            return $"[{Series.Title} : S{SeasonNumber:00}E{EpisodeNumber:00}]";
        }
    }
}