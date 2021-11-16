namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class SeasonSearchCriteria : SearchCriteriaBase
    {
        public int SeasonNumber { get; set; }

        public override string ToString()
        {
            return $"[{Series.Title} : S{SeasonNumber:00}]";
        }
    }
}