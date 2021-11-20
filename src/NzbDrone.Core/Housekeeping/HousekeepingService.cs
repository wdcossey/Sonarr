using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping
{
    public class HousekeepingService : IExecuteAsync<HousekeepingCommand>
    {
        private readonly IEnumerable<IHousekeepingTask> _housekeepers;
        private readonly ILogger<HousekeepingService> _logger;
        private readonly IMainDatabase _mainDb;

        public HousekeepingService(IEnumerable<IHousekeepingTask> housekeepers, IMainDatabase mainDb, ILogger<HousekeepingService> logger)
        {
            _housekeepers = housekeepers;
            _logger = logger;
            _mainDb = mainDb;
        }

        private void Clean()
        {
            _logger.LogInformation("Running housecleaning tasks");

            foreach (var housekeeper in _housekeepers)
            {
                try
                {
                    _logger.LogDebug("Starting {Name}", housekeeper.GetType().Name);
                    housekeeper.Clean();
                    _logger.LogDebug("Completed {Name}", housekeeper.GetType().Name);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running housekeeping task: {Name}", housekeeper.GetType().Name);
                }
            }

            // Vacuuming the log db isn't needed since that's done in a separate housekeeping task
            _logger.LogDebug("Compressing main database after housekeeping");
            _mainDb.Vacuum();
        }

        public Task ExecuteAsync(HousekeepingCommand message)
        {
            Clean();
            return Task.CompletedTask;
        }
    }
}
