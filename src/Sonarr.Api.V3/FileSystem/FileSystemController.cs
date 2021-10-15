using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;
using Sonarr.Http.Attributes;

namespace Sonarr.Api.V3.FileSystem
{
    [SonarrApiRoute("filesystem", RouteVersion.V3)]
    public class FileSystemController : ControllerBase
    {
        private readonly IFileSystemLookupService _fileSystemLookupService;
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskScanService _diskScanService;

        public FileSystemController(
            IFileSystemLookupService fileSystemLookupService,
            IDiskProvider diskProvider,
            IDiskScanService diskScanService)
        {
            _fileSystemLookupService = fileSystemLookupService;
            _diskProvider = diskProvider;
            _diskScanService = diskScanService;
        }

        [HttpGet]
        public IActionResult GetContents([FromQuery] string path, [FromQuery] bool includeFiles = false, [FromQuery] bool allowFoldersWithoutTrailingSlashes = false)
            => Ok(_fileSystemLookupService.LookupContents(path, includeFiles, allowFoldersWithoutTrailingSlashes));

        [HttpGet("type")]
        public IActionResult GetEntityType([FromQuery] string path)
        {
            if (_diskProvider.FileExists(path))
                return Ok(new { type = "file" });

            //Return folder even if it doesn't exist on disk to avoid leaking anything from the UI about the underlying system
            return Ok(new { type = "folder" });
        }

        [HttpGet("mediafiles")]
        public IActionResult GetMediaFiles([FromQuery] string path)
        {
            if (!_diskProvider.FolderExists(path))
                return Ok(Array.Empty<string>());

            return Ok(_diskScanService.GetVideoFiles(path).Select(f =>
                new
                {
                    Path = f,
                    RelativePath = path.GetRelativePath(f),
                    Name = Path.GetFileName(f)
                }));
        }
    }
}
