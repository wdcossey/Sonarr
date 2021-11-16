using Microsoft.Extensions.Logging;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Aggregation.Aggregators.Augmenters.Quality
{
    public class AugmentQualityFromMediaInfo : IAugmentQuality
    {
        private readonly ILogger<AugmentQualityFromMediaInfo> _logger;

        public int Order => 4;
        public string Name => "MediaInfo";

        public AugmentQualityFromMediaInfo(ILogger<AugmentQualityFromMediaInfo> logger)
            => _logger = logger;

        public AugmentQualityResult AugmentQuality(LocalEpisode localEpisode, DownloadClientItem downloadClientItem)
        {
            if (localEpisode.MediaInfo == null)
            {
                return null;
            }

            var width = localEpisode.MediaInfo.Width;
            var height = localEpisode.MediaInfo.Height;


            if (width >= 3200 || height >= 2100)
            {
                _logger.LogTrace("Resolution {Width}x{Height} considered 2160p", width, height);
                return AugmentQualityResult.ResolutionOnly(2160, Confidence.MediaInfo);
            }

            if (width >= 1800 || height >= 1000)
            {
                _logger.LogTrace("Resolution {Width}x{Height} considered 1080p", width, height);
                return AugmentQualityResult.ResolutionOnly(1080, Confidence.MediaInfo);
            }

            if (width >= 1200 || height >= 700)
            {
                _logger.LogTrace("Resolution {Width}x{Height} considered 720p", width, height);
                return AugmentQualityResult.ResolutionOnly(720, Confidence.MediaInfo);
            }

            if (width > 0 && height > 0)
            {
                _logger.LogTrace("Resolution {Width}x{Height} considered 480p", width, height);
                return AugmentQualityResult.ResolutionOnly(480, Confidence.MediaInfo);
            }

            _logger.LogTrace("Resolution {Width}x{Height}", width, height);

            return null;
        }
    }
}
