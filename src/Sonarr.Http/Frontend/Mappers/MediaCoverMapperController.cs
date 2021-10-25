using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using Sonarr.Http.Extensions;

namespace Sonarr.Http.Frontend.Mappers
{
    //[ResponseCache]
    [Route("MediaCover")]
    public class MediaCoverMapperController : PhysicalFileControllerBase
    {
        private static readonly Regex RegexResizedImage = new Regex(@"-\d+\.jpg($|\?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;

        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        public MediaCoverMapperController(
            ILogger<MediaCoverMapperController> logger,
            IAppFolderInfo appFolderInfo,
            IDiskProvider diskProvider)
            : base(logger)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
        }

        [HttpGet("{seriesId:int:required:regex(\\d+)}/{filename:required:regex(((.+))\\.((jpg|png|gif)))}")]
        public IActionResult GetMediaCoverFile(int seriesId, string fileName)
        {
            var filePath = GetFilePath(seriesId, fileName);
            return GetPhysicalFile(filePath);
        }

        private string GetFilePath(int seriesId, string fileName)
        {
           var resourcePath = Path.Combine(_appFolderInfo.GetAppDataPath(), "MediaCover", $"{seriesId}", fileName);

            if (!_diskProvider.FileExists(resourcePath) || _diskProvider.GetFileSize(resourcePath) == 0)
            {
                var baseResourcePath = RegexResizedImage.Replace(resourcePath, ".jpg$1");
                if (baseResourcePath != resourcePath)
                {
                    return baseResourcePath;
                }
            }

            return resourcePath;
        }
    }
}
