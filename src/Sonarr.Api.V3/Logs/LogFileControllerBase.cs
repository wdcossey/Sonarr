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

        public LogFileControllerBase(
                IDiskProvider diskProvider,
                IConfigFileProvider configFileProvider)
            //: base("log/file" + route)
        {
            _diskProvider = diskProvider;
            _configFileProvider = configFileProvider;
            //GetResourceAll = GetLogFilesResponse;

            //Get(LOGFILE_ROUTE,  options => GetLogFileResponse(options.filename));
        }

        [HttpGet]
        public IActionResult GetLogFilesResponse()
        {
            var result = new List<LogFileResource>();

            var files = GetLogFiles().ToList();

            /*
             "contentsUrl": "/api/v3/log/file/sonarr.txt",
        "downloadUrl": ,
             */
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var filename = Path.GetFileName(file);

                result.Add(new LogFileResource
                {
                    Id = i + 1,
                    Filename = filename,
                    LastWriteTime = _diskProvider.FileGetLastWrite(file),
                    ContentsUrl = $"{_configFileProvider.UrlBase}/api/v3/{"Resource"}/{filename}",  //"/api/v3/log/file/sonarr.txt"
                    DownloadUrl = $"{_configFileProvider.UrlBase}/{DownloadUrlRoot}/{filename}"     //"/logfile/sonarr.txt"
                });
            }

            return Ok(result.OrderByDescending(l => l.LastWriteTime).ToList());
        }

        [Route("{filename:regex([[-.a-zA-Z0-9]]+?\\.txt)}")]
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