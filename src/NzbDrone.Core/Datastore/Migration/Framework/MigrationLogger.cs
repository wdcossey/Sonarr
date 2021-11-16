using System;
using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;

namespace NzbDrone.Core.Datastore.Migration.Framework
{
    public class MigrationLogger : IAnnouncer
    {
        private readonly ILogger<MigrationLogger> _logger;

        public MigrationLogger(ILogger<MigrationLogger> logger)
            => _logger = logger;

        public void Heading(string message)
            => _logger.LogInformation("*** {Message} ***", message);

        public void Say(string message)
            => _logger.LogDebug("{Message}", message);

        public void Emphasize(string message)
            => _logger.LogWarning("{Message}", message);

        public void Sql(string sql)
            => _logger.LogDebug("{Sql}", sql);

        public void ElapsedTime(TimeSpan timeSpan)
            => _logger.LogDebug("Took: {TimeSpan}", timeSpan);

        public void Error(string message)
            => _logger.LogError("{Message}", message);


        public void Error(Exception exception)
            => _logger.LogError("{Exception}", exception);

        public void Write(string message, bool escaped)
            => _logger.LogInformation("{Message}", message);
    }
}
