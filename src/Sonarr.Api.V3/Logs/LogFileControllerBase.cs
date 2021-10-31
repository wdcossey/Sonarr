using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;

namespace Sonarr.Api.V3.Logs
{
    public abstract class LogFileControllerBase : ControllerBase
    {
        protected const string LOGFILE_ROUTE = @"/(?<filename>[-.a-zA-Z0-9]+?\.txt)";

        private readonly IDiskProvider _diskProvider;
        private readonly IConfigFileProvider _configFileProvider;

        protected LogFileControllerBase(
            IDiskProvider diskProvider,
            IConfigFileProvider configFileProvider)
        {
            _diskProvider = diskProvider;
            _configFileProvider = configFileProvider;
        }

        [HttpGet]
        public IActionResult GetLogFilesResponse()
        {
            var index = 0;
            var result = GetLogFiles()?.Select(file =>
            {
                var filename = Path.GetFileName(file);
                return new LogFileResource
                {
                    Id = ++index,
                    Filename = filename,
                    LastWriteTime = _diskProvider.FileGetLastWrite(file),
                    ContentsUrl = $"{_configFileProvider.UrlBase}{Request.Path}/{filename}",
                    DownloadUrl = $"{_configFileProvider.UrlBase}/{DownloadUrlRoot}/{filename}"
                };
            });

            return Ok(result?.OrderByDescending(l => l.LastWriteTime) ?? Enumerable.Empty<LogFileResource>());
        }

        [HttpGet("{filename:regex([[-.a-zA-Z0-9]]+?\\.txt)}")]
        public IActionResult GetLogFileResponse(string filename)
        {
            var filePath = GetLogFilePath(filename);

            if (!_diskProvider.FileExists(filePath))
                return NotFound();

            var provider = new FileExtensionContentTypeProvider();

            if(!provider.TryGetContentType(filePath, out var contentType))
                contentType = MediaTypeNames.Application.Octet;

            return PhysicalFile(filePath, contentType);
        }

        protected abstract IEnumerable<string> GetLogFiles();

        protected abstract string GetLogFilePath(string filename);

        protected abstract string DownloadUrlRoot { get; }
    }
}