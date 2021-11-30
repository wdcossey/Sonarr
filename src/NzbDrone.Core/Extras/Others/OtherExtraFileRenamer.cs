﻿using System.IO;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Extras.Others
{
    public interface IOtherExtraFileRenamer
    {
        void RenameOtherExtraFile(Series series, string path);
    }

    public class OtherExtraFileRenamer : IOtherExtraFileRenamer
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IOtherExtraFileService _otherExtraFileService;

        public OtherExtraFileRenamer(IOtherExtraFileService otherExtraFileService,
            IRecycleBinProvider recycleBinProvider,
                                     IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _otherExtraFileService = otherExtraFileService;
        }

        public void RenameOtherExtraFile(Series series, string path)
        {
            if (!_diskProvider.FileExists(path))
            {
                return;
            }

            var relativePath = series.Path.GetRelativePath(path);

            var otherExtraFile = _otherExtraFileService.FindByPath(relativePath);
            if (otherExtraFile != null)
            {
                var newPath = path + "-orig";

                // Recycle an existing -orig file.
                RemoveOtherExtraFile(series, newPath);

                // Rename the file to .*-orig
                _diskProvider.MoveFile(path, newPath);
                otherExtraFile.RelativePath = relativePath + "-orig";
                otherExtraFile.Extension += "-orig";
                _otherExtraFileService.Upsert(otherExtraFile);
            }
        }

        private void RemoveOtherExtraFile(Series series, string path)
        {
            if (!_diskProvider.FileExists(path))
            {
                return;
            }

            var relativePath = series.Path.GetRelativePath(path);

            var otherExtraFile = _otherExtraFileService.FindByPath(relativePath);
            if (otherExtraFile != null)
            {
                var subfolder = Path.GetDirectoryName(relativePath);
                _recycleBinProvider.DeleteFile(path, subfolder);
            }
        }
    }
}
