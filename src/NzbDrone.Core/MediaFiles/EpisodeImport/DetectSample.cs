﻿using System;
using System.IO;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MediaFiles.EpisodeImport
{
    public interface IDetectSample
    {
        DetectSampleResult IsSample(Series series, string path, bool isSpecial);
    }

    public class DetectSample : IDetectSample
    {
        private readonly IVideoFileInfoReader _videoFileInfoReader;
        private readonly ILogger<DetectSample> _logger;

        public DetectSample(IVideoFileInfoReader videoFileInfoReader, ILogger<DetectSample> logger)
        {
            _videoFileInfoReader = videoFileInfoReader;
            _logger = logger;
        }

        public DetectSampleResult IsSample(Series series, string path, bool isSpecial)
        {
            if (isSpecial)
            {
                _logger.LogDebug("Special, skipping sample check");
                return DetectSampleResult.NotSample;
            }

            var extension = Path.GetExtension(path);

            if (extension != null && extension.Equals(".flv", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogDebug("Skipping sample check for .flv file");
                return DetectSampleResult.NotSample;
            }

            if (extension != null && extension.Equals(".strm", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogDebug("Skipping sample check for .strm file");
                return DetectSampleResult.NotSample;
            }

            // TODO: Use MediaInfo from the import process, no need to re-process the file again here
            var runTime = _videoFileInfoReader.GetRunTime(path);

            if (!runTime.HasValue)
            {
                _logger.LogError("Failed to get runtime from the file, make sure mediainfo is available");
                return DetectSampleResult.Indeterminate;
            }

            var minimumRuntime = GetMinimumAllowedRuntime(series);

            if (runTime.Value.TotalMinutes.Equals(0))
            {
                _logger.LogError("[{Path}] has a runtime of 0, is it a valid video file?", path);
                return DetectSampleResult.Sample;
            }

            if (runTime.Value.TotalSeconds < minimumRuntime)
            {
                _logger.LogDebug("[{Path}] appears to be a sample. Runtime: {RunTime} seconds. Expected at least: {MinimumRuntime} seconds", path, runTime, minimumRuntime);
                return DetectSampleResult.Sample;
            }

            _logger.LogDebug("Runtime is over 90 seconds");
            return DetectSampleResult.NotSample;
        }

        private int GetMinimumAllowedRuntime(Series series)
        {
            //Anime short - 15 seconds
            if (series.Runtime <= 3)
                return 15;

            //Webisodes - 90 seconds
            if (series.Runtime <= 10)
                return 90;

            //30 minute episodes - 5 minutes
            if (series.Runtime <= 30)
                return 300;

            //60 minute episodes - 10 minutes
            return 600;
        }
    }
}
