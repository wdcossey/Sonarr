using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.Logs
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ApiController]
    [SonarrApiRoute("log/file", RouteVersion.V3)]
    public class LogFileController : LogFileControllerBase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;

        public LogFileController(IAppFolderInfo appFolderInfo,
                             IDiskProvider diskProvider,
                             IConfigFileProvider configFileProvider)
            : base(diskProvider, configFileProvider)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
        }

        protected override IEnumerable<string> GetLogFiles()
            => _diskProvider.GetFiles(_appFolderInfo.GetLogFolder(), SearchOption.TopDirectoryOnly);

        protected override string GetLogFilePath(string filename)
            => Path.Combine(_appFolderInfo.GetLogFolder(), filename);

        protected override string DownloadUrlRoot
            => "logfile";
    }
}