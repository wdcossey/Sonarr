using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(90)]
    public class update_kickass_url : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql(
                "UPDATE Indexers SET Settings = Replace(Settings, 'kickass.so', 'kat.cr') WHERE Implementation = 'KickassTorrents';" +
                "UPDATE Indexers SET Settings = Replace(Settings, 'kickass.to', 'kat.cr') WHERE Implementation = 'KickassTorrents';" +
                "UPDATE Indexers SET Settings = Replace(Settings, 'http://', 'https://') WHERE Implementation = 'KickassTorrents';"
           );
        }
    }

    public abstract class IndexerDefinition90
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Implementation { get; set; }
        public string ConfigContract { get; set; }
        public bool EnableRss { get; set; }
        public bool EnableSearch { get; set; }
    }

    public class IndexerDefinition90<TSettings> : IndexerDefinition90
        where TSettings : class
    {
        public TSettings Settings { get; set; }
    }

    public class KickassTorrentsSettings90
    {
        public string BaseUrl { get; set; }
        public bool VerifiedOnly { get; set; }
    }
}
