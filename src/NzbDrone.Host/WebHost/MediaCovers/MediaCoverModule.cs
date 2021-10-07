using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Nancy;
using Nancy.Responses;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using HttpStatusCode = System.Net.HttpStatusCode;
using MimeTypes = MimeKit.MimeTypes;

namespace NzbDrone.Host.WebHost.MediaCovers
{
    public class MediaCoverModule: WebApiController
    {

        private static readonly Regex RegexResizedImage = new Regex(@"-\d+\.jpg$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const string MEDIA_COVER_ROUTE = @"/(?<seriesId>\d+)/(?<filename>(.+)\.(jpg|png|gif))";

        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;

        public MediaCoverModule(IAppFolderInfo appFolderInfo, IDiskProvider diskProvider)
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
        }

        [Route(HttpVerbs.Get, "/{seriesId}/{filename}")]
        public async Task GetMediaCoverAsync(int seriesId, string filename)
        {
            var filePath = Path.Combine(_appFolderInfo.GetAppDataPath(), "MediaCover", seriesId.ToString(), filename);

            if (!_diskProvider.FileExists(filePath) || _diskProvider.GetFileSize(filePath) == 0)
            {
                // Return the full sized image if someone requests a non-existing resized one.
                // TODO: This code can be removed later once everyone had the update for a while.
                var basefilePath = RegexResizedImage.Replace(filePath, ".jpg");
                if (basefilePath == filePath || !_diskProvider.FileExists(basefilePath))
                {
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }
                filePath = basefilePath;
            }

            await using var stream = HttpContext.OpenResponseStream(false, true);
            await File.OpenRead(filePath).CopyToAsync(stream);
            HttpContext.Response.ContentType = MimeTypes.GetMimeType(filePath);

        }
    }
}
