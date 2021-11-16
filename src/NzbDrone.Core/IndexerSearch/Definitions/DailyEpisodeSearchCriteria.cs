using System;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class DailyEpisodeSearchCriteria : SearchCriteriaBase
    {
        public DateTime AirDate { get; set; }

        public override string ToString()
        {
            return $"[{Series.Title} : {AirDate:yyyy-MM-dd}]";
        }
    }
}
