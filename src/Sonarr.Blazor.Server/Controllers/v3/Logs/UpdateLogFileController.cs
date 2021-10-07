using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;

namespace Sonarr.Blazor.Server.Controllers.v3.Logs
{
    [ApiController]
    [Route("/api/v3/log/file/update")]
    public class UpdateLogFileController : LogFileControllerBase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;

        public UpdateLogFileController(IAppFolderInfo appFolderInfo,
                                   IDiskProvider diskProvider,
                                   IConfigFileProvider configFileProvider)
            : base(diskProvider, configFileProvider/*, "/update"*/)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
        }

        protected override IEnumerable<string> GetLogFiles()
        {
            if (!_diskProvider.FolderExists(_appFolderInfo.GetUpdateLogFolder())) return Enumerable.Empty<string>();

            return _diskProvider.GetFiles(_appFolderInfo.GetUpdateLogFolder(), SearchOption.TopDirectoryOnly)
                                     .Where(f => Regex.IsMatch(Path.GetFileName(f), LOGFILE_ROUTE.TrimStart('/'), RegexOptions.IgnoreCase))
                                     .ToList();
        }

        protected override string GetLogFilePath(string filename)
        {
            return Path.Combine(_appFolderInfo.GetUpdateLogFolder(), filename);
        }

        //TODO: This is a replacement for `Sonarr.Http.Frontend.Mappers.UpdateLogFileMapper`
        [Route("/updatelogfile")]
        [HttpGet("/updatelogfile/{filename:required:regex([[-.a-zA-Z0-9]]+?\\.txt)}")]
        public IActionResult DownloadLogFile(string filename)
        {
            var filePath = GetLogFilePath(filename);

            if (!_diskProvider.FileExists(filePath))
                return NotFound();

            var provider = new FileExtensionContentTypeProvider();

            if(!provider.TryGetContentType(filename, out var contentType))
                contentType = MediaTypeNames.Application.Octet;

            return new PhysicalFileResult(filePath, contentType);
        }

        protected override string DownloadUrlRoot => "updatelogfile";
    }
}