using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NzbDrone.Api.Logs;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using Sonarr.Http;

namespace NzbDrone.Host.WebHost.Logs
{
    public class LogFileModule: WebApiController
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigFileProvider _configFileProvider;

        public LogFileModule(IAppFolderInfo appFolderInfo,
            IDiskProvider diskProvider,
            IConfigFileProvider configFileProvider)
            //: base(diskProvider, configFileProvider, "")
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _configFileProvider = configFileProvider;
        }

        [Route(HttpVerbs.Get, "/")]
        public Task<List<LogFileResource>> GetLogFilesResponse()
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
                    ContentsUrl = string.Format("{0}/{1}",  this.Request.Url.AbsolutePath /*Resource*/, filename),
                    DownloadUrl = string.Format("{0}/{1}/{2}", _configFileProvider.UrlBase, "logfile"/*DownloadUrlRoot*/, filename)
                });
            }

            return Task.FromResult<List<LogFileResource>>(result.OrderByDescending(l => l.LastWriteTime).ToList());
        }

        private IEnumerable<string> GetLogFiles()
        {
            var result = _diskProvider.GetFiles(_appFolderInfo.GetLogFolder(), SearchOption.TopDirectoryOnly);
            return result;
        }


        /*protected override <string> GetLogFiles()
        {
            return _diskProvider.GetFiles(_appFolderInfo.GetLogFolder(), SearchOption.TopDirectoryOnly);
        }

        protected override string GetLogFilePath(string filename)
        {
            return Path.Combine(_appFolderInfo.GetLogFolder(), filename);
        }

        protected override string DownloadUrlRoot
        {
            get
            {
                return "logfile";
            }
        }*/
    }
}
