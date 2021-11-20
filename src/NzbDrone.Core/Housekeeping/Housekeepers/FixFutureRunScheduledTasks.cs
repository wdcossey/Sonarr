using System;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class FixFutureRunScheduledTasks : IHousekeepingTask
    {
        private readonly IMainDatabase _database;
        private readonly ILogger<FixFutureRunScheduledTasks> _logger;

        public FixFutureRunScheduledTasks(IMainDatabase database, ILogger<FixFutureRunScheduledTasks> logger)
        {
            _database = database;
            _logger = logger;
        }

        public void Clean()
        {
            if (BuildInfo.IsDebug)
            {
                _logger.LogDebug("Not running scheduled task last execution cleanup during debug");
            }

            var mapper = _database.GetDataMapper();
            mapper.AddParameter("time", DateTime.UtcNow);

            mapper.ExecuteNonQuery(@"UPDATE ScheduledTasks
                                     SET LastExecution = @time
                                     WHERE LastExecution > @time");
        }
    }
}
