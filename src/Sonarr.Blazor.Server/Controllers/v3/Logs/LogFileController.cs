using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;

namespace Sonarr.Blazor.Server.Controllers.v3.Logs
{
    [ApiController]
    [Route("/api/v3/log/file")]
    public class LogFileController : LogFileControllerBase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;

        public LogFileController(IAppFolderInfo appFolderInfo,
                             IDiskProvider diskProvider,
                             IConfigFileProvider configFileProvider)
            : base(diskProvider, configFileProvider/*, ""*/)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
        }

        protected override IEnumerable<string> GetLogFiles()
        {
            return _diskProvider.GetFiles(_appFolderInfo.GetLogFolder(), SearchOption.TopDirectoryOnly);
        }

        protected override string GetLogFilePath(string filename)
        {
            return Path.Combine(_appFolderInfo.GetLogFolder(), filename);
        }

        //TODO: This is a replacement for `Sonarr.Http.Frontend.Mappers.LogFileMapper`
        [Route("/logfile")]
        [HttpGet("/logfile/{filename:required:regex([[-.a-zA-Z0-9]]+?\\.txt)}")]
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

        protected override string DownloadUrlRoot => "logfile";
    }
}