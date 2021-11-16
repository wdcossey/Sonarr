using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.DiskSpace
{
    public interface IDiskSpaceService
    {
        List<DiskSpace> GetFreeSpace();
    }

    public class DiskSpaceService : IDiskSpaceService
    {
        private readonly ISeriesService _seriesService;
        private readonly IDiskProvider _diskProvider;
        private readonly ILogger<DiskSpaceService> _logger;

        private static readonly Regex _regexSpecialDrive = new Regex("^/var/lib/(docker|rancher|kubelet)(/|$)|^/(boot|etc)(/|$)|/docker(/var)?/aufs(/|$)", RegexOptions.Compiled);

        public DiskSpaceService(ISeriesService seriesService, IDiskProvider diskProvider, ILogger<DiskSpaceService> logger)
        {
            _seriesService = seriesService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<DiskSpace> GetFreeSpace()
        {
            var importantRootFolders = GetSeriesRootPaths().Distinct().ToList();

            var optionalRootFolders = GetFixedDisksRootPaths().Except(importantRootFolders).Distinct().ToList();

            var diskSpace = GetDiskSpace(importantRootFolders).Concat(GetDiskSpace(optionalRootFolders, true)).ToList();

            return diskSpace;
        }

        private IEnumerable<string> GetSeriesRootPaths()
        {
            return _seriesService.GetAllSeriesPaths()
                .Where(s => _diskProvider.FolderExists(s))
                .Select(s => _diskProvider.GetPathRoot(s))
                .Distinct();
        }

        private IEnumerable<string> GetFixedDisksRootPaths()
        {
            return _diskProvider.GetMounts()
                .Where(d => d.DriveType == DriveType.Fixed)
                .Where(d => !_regexSpecialDrive.IsMatch(d.RootDirectory))
                .Select(d => d.RootDirectory);
        }

        private IEnumerable<DiskSpace> GetDiskSpace(IEnumerable<string> paths, bool suppressWarnings = false)
        {
            foreach (var path in paths)
            {
                DiskSpace diskSpace = null;

                try
                {
                    var freeSpace = _diskProvider.GetAvailableSpace(path);
                    var totalSpace = _diskProvider.GetTotalSize(path);

                    if (!freeSpace.HasValue || !totalSpace.HasValue)
                    {
                        continue;
                    }

                    diskSpace = new DiskSpace
                                {
                                    Path = path,
                                    FreeSpace = freeSpace.Value,
                                    TotalSpace = totalSpace.Value
                                };

                    diskSpace.Label = _diskProvider.GetVolumeLabel(path);
                }
                catch (Exception ex)
                {
                    if (!suppressWarnings)
                    {
                        _logger.LogWarning(ex, "Unable to get free space for: {Path}", path);
                    }
                }

                if (diskSpace != null)
                {
                    yield return diskSpace;
                }
            }
        }
    }
}
