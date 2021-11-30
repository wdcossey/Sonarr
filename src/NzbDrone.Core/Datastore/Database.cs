using System;
using Marr.Data;
using Microsoft.Extensions.Logging;

namespace NzbDrone.Core.Datastore
{
    public interface IDatabase
    {
        IDataMapper GetDataMapper();
        Version Version { get; }
        void Vacuum();
    }

    public class Database : IDatabase
    {
        private readonly string _databaseName;
        private readonly Func<IDataMapper> _datamapperFactory;
        private readonly ILogger _logger;
        
        public Database(ILogger logger, string databaseName, Func<IDataMapper> datamapperFactory)
        {
            _databaseName = databaseName;
            _datamapperFactory = datamapperFactory;
            _logger = logger;
        }

        public IDataMapper GetDataMapper()
        {
            return _datamapperFactory();
        }

        public Version Version
        {
            get
            {
                var version = _datamapperFactory().ExecuteScalar("SELECT sqlite_version()").ToString();
                return new Version(version);
            }
        }

        public void Vacuum()
        {
            try
            {
                _logger.LogInformation("Vacuuming {DatabaseName} database", _databaseName);
                _datamapperFactory().ExecuteNonQuery("Vacuum;");
                _logger.LogInformation("{DatabaseName} database compressed", _databaseName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An Error occurred while vacuuming database.");
            }
        }
    }
}
