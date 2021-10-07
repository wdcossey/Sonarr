using System;
using Sonarr.Blazor.Shared;

namespace Sonarr.Blazor.Server.Controllers.v3.Logs
{
    public class LogFileResource : RestResource
    {
        public string Filename { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string ContentsUrl { get; set; }
        public string DownloadUrl { get; set; }
    }
}
