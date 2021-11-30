namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class DailySeasonSearchCriteria : SearchCriteriaBase
    {
        public int Year { get; set; }

        public override string ToString()
        {
            return $"[{Series.Title} : {Year}]";
        }
    }
}
