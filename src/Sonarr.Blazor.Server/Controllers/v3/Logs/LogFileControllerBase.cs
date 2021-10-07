using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;

namespace Sonarr.Blazor.Server.Controllers.v3.Logs
{
    [ApiController]
    //[Route("/api/v3/log/file")]
    public abstract class LogFileControllerBase : ControllerBase // SonarrRestModule<LogFileResource>
    {
        protected const string LOGFILE_ROUTE = @"/(?<filename>[-.a-zA-Z0-9]+?\.txt)";

        private readonly IDiskProvider _diskProvider;
        private readonly IConfigFileProvider _configFileProvider;

        public LogFileControllerBase(IDiskProvider diskProvider,
                                 IConfigFileProvider configFileProvider/*,
                                 string route*/)
            //: base("log/file" + route)
        {
            _diskProvider = diskProvider;
            _configFileProvider = configFileProvider;
            //GetResourceAll = GetLogFilesResponse;

            //Get(LOGFILE_ROUTE,  options => GetLogFileResponse(options.filename));
        }

        [HttpGet]
        public List<LogFileResource> GetLogFilesResponse()
        {
            var result = new List<LogFileResource>();

            var files = GetLogFiles().ToList();

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var filename = Path.GetFileName(file);

                result.Add(new LogFileResource
                {
                    Id = i + 1,
                    Filename = filename,
                    LastWriteTime = _diskProvider.FileGetLastWrite(file),
                    ContentsUrl = string.Format("{0}/{0}/{1}", _configFileProvider.UrlBase, this.Request.Path /*Resource*/, filename), //TODO: Fix me!
                    DownloadUrl = string.Format("{0}/{1}/{2}", _configFileProvider.UrlBase, DownloadUrlRoot, filename)
                });
            }

            return result.OrderByDescending(l => l.LastWriteTime).ToList();
        }

        [HttpGet("{filename:required:regex([[-.a-zA-Z0-9]]+?\\.txt)}")]
        public IActionResult GetLogFileResponse([FromQuery] string filename)
        {
            var filePath = GetLogFilePath(filename);

            if (!_diskProvider.FileExists(filePath))
                return NotFound();

            return new PhysicalFileResult(filePath, MediaTypeNames.Text.Plain); //new TextResponse(data);
        }

        protected abstract IEnumerable<string> GetLogFiles();
        protected abstract string GetLogFilePath(string filename);

        protected abstract string DownloadUrlRoot { get; }
    }
}