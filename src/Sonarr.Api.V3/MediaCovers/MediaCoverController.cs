using System.IO;
using System.Net.Mime;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.MediaCovers
{
    [ApiController]
    [SonarrApiRoute("MediaCover", RouteVersion.V3)]
    public class MediaCoverController : ControllerBase
    {
        private static readonly Regex RegexResizedImage = new(@"-\d+\.jpg$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;

        public MediaCoverController(IAppFolderInfo appFolderInfo, IDiskProvider diskProvider)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
        }

        [HttpGet("{seriesId:int:regex(\\d+)}/{filename:regex(((.+))\\.((jpg|png|gif)))}")]
        public IActionResult GetMediaCover(int seriesId, string filename)
        {
            if (seriesId <= 0 || string.IsNullOrWhiteSpace(filename))
                return NotFound();

            var filePath = Path.Combine(_appFolderInfo.GetAppDataPath(), "MediaCover", seriesId.ToString(), filename);

            if (!_diskProvider.FileExists(filePath) || _diskProvider.GetFileSize(filePath) == 0)
            {
                // Return the full sized image if someone requests a non-existing resized one.
                // TODO: This code can be removed later once everyone had the update for a while.
                var basefilePath = RegexResizedImage.Replace(filePath, ".jpg");
                if (basefilePath == filePath || !_diskProvider.FileExists(basefilePath))
                {
                    return NotFound();
                }
                filePath = basefilePath;
            }

            var provider = new FileExtensionContentTypeProvider();

            if(!provider.TryGetContentType(filePath, out var contentType))
                contentType = MediaTypeNames.Application.Octet;

            return PhysicalFile(filePath, contentType);
        }
    }
}
