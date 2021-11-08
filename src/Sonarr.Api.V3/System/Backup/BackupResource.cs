using System;
using NzbDrone.Core.Backup;
using Sonarr.Http.Attributes;
using Sonarr.Http.REST;

namespace Sonarr.Api.V3.System.Backup
{
    [BroadcastName("Backup")]
    public class BackupResource : RestResource
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public BackupType Type { get; set; }
        public DateTime Time { get; set; }
    }
}
