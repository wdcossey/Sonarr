﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    [SonarrApiRoute("log/file/update", RouteVersion.V3)]
    public class UpdateLogFileController : LogFileControllerBase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;

        public UpdateLogFileController(IAppFolderInfo appFolderInfo,
                                   IDiskProvider diskProvider,
                                   IConfigFileProvider configFileProvider)
            : base(diskProvider, configFileProvider)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
        }

        protected override IEnumerable<string> GetLogFiles()
        {
            if (!_diskProvider.FolderExists(_appFolderInfo.GetUpdateLogFolder()))
                return Enumerable.Empty<string>();

            return _diskProvider.GetFiles(_appFolderInfo.GetUpdateLogFolder(), SearchOption.TopDirectoryOnly)
                .Where(f => Regex.IsMatch(Path.GetFileName(f), LOGFILE_ROUTE.TrimStart('/'), RegexOptions.IgnoreCase))
                .ToList();
        }

        protected override string GetLogFilePath(string filename)
            => Path.Combine(_appFolderInfo.GetUpdateLogFolder(), filename);

        protected override string DownloadUrlRoot
            => "updatelogfile";
    }
}