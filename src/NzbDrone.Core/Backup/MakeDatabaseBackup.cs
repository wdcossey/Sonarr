using System.Data.SQLite;
using System.IO;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Backup
{
    public interface IMakeDatabaseBackup
    {
        void BackupDatabase(IDatabase database, string targetDirectory);
    }

    public class MakeDatabaseBackup : IMakeDatabaseBackup
    {
        private readonly ILogger<MakeDatabaseBackup> _logger;

        public MakeDatabaseBackup(ILogger<MakeDatabaseBackup> logger)
            => _logger = logger;

        public void BackupDatabase(IDatabase database, string targetDirectory)
        {
            var sourceConnectionString = database.GetDataMapper().ConnectionString;
            var backupConnectionStringBuilder = new SQLiteConnectionStringBuilder(sourceConnectionString);

            backupConnectionStringBuilder.DataSource = Path.Combine(targetDirectory, Path.GetFileName(backupConnectionStringBuilder.DataSource));
            // We MUST use journal mode instead of WAL coz WAL has issues when page sizes change. This should also automatically deal with the -journal and -wal files during restore.
            backupConnectionStringBuilder.JournalMode = SQLiteJournalModeEnum.Truncate;

            using (var sourceConnection = (SQLiteConnection)SQLiteFactory.Instance.CreateConnection())
            using (var backupConnection = (SQLiteConnection)SQLiteFactory.Instance.CreateConnection())
            {
                sourceConnection.ConnectionString = sourceConnectionString;
                backupConnection.ConnectionString = backupConnectionStringBuilder.ToString();

                sourceConnection.Open();
                backupConnection.Open();

                sourceConnection.BackupDatabase(backupConnection, "main", "main", -1, null, 500);

                // The backup changes the journal_mode, force it to truncate again.
                using (var command = backupConnection.CreateCommand())
                {
                    command.CommandText = "pragma journal_mode=truncate";
                    command.ExecuteNonQuery();
                }

                // Make sure there are no lingering connections.
                SQLiteConnection.ClearAllPools();
            }
        }
    }
}
