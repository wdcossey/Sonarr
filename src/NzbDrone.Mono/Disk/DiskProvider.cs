using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mono.Unix;
using Mono.Unix.Native;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Mono.Disk
{
    public class DiskProvider : DiskProviderBase
    {
        //private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(DiskProvider));

        private readonly IProcMountProvider _procMountProvider;
        private readonly ISymbolicLinkResolver _symLinkResolver;
        private readonly ICreateRefLink _createRefLink;

        // Mono supports sending -1 for a uint to indicate that the owner or group should not be set
        // `unchecked((uint)-1)` and `uint.MaxValue` are the same thing.
        private const uint UNCHANGED_ID = uint.MaxValue;

        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        public DiskProvider(ILogger<DiskProvider> logger, IProcMountProvider procMountProvider, ISymbolicLinkResolver symLinkResolver, ICreateRefLink createRefLink)
            : base(logger)
        {
            _procMountProvider = procMountProvider;
            _symLinkResolver = symLinkResolver;
            _createRefLink = createRefLink;
        }

        public override IMount GetMount(string path)
        {
            path = _symLinkResolver.GetCompleteRealPath(path);

            return base.GetMount(path);
        }

        public override long? GetAvailableSpace(string path)
        {
            Ensure.That(path, () => path).IsValidPath();

            var mount = GetMount(path);

            if (mount == null)
            {
                Logger.LogDebug("Unable to get free space for '{Path}', unable to find suitable drive", path);
                return null;
            }

            return mount.AvailableFreeSpace;
        }

        public override void InheritFolderPermissions(string filename)
        {

        }

        public override void SetEveryonePermissions(string filename)
        {

        }

        public override void SetFilePermissions(string path, string mask, string group)
        {
            var permissions = NativeConvert.FromOctalPermissionString(mask);

            SetPermissions(path, mask, group, permissions);
        }

        public override void SetPermissions(string path, string mask, string group)
        {
            var permissions = NativeConvert.FromOctalPermissionString(mask);

            if (File.Exists(path))
            {
                permissions = GetFilePermissions(permissions);
            }

            SetPermissions(path, mask, group, permissions);
        }

        protected void SetPermissions(string path, string mask, string group, FilePermissions permissions)
        {
            Logger.LogDebug("Setting permissions: {Mask} on {Path}", mask, path);

            // Preserve non-access permissions
            if (Syscall.stat(path, out var curStat) < 0)
            {
                var error = Stdlib.GetLastError();

                throw new LinuxPermissionsException("Error getting current permissions: " + error);
            }

            // Preserve existing non-access permissions unless mask is 4 digits
            if (mask.Length < 4)
            {
                permissions |= curStat.st_mode & ~FilePermissions.ACCESSPERMS;
            }

            if (Syscall.chmod(path, permissions) < 0)
            {
                var error = Stdlib.GetLastError();

                throw new LinuxPermissionsException("Error setting permissions: " + error);
            }

            var groupId = GetGroupId(group);

            if (Syscall.chown(path, unchecked((uint)-1), groupId) < 0)
            {
                var error = Stdlib.GetLastError();

                throw new LinuxPermissionsException("Error setting group: " + error);
            }
        }

        private static FilePermissions GetFilePermissions(FilePermissions permissions)
        {
            permissions &= ~(FilePermissions.S_IXUSR | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH);

            return permissions;
        }

        public override bool IsValidFolderPermissionMask(string mask)
        {
            try
            {
                var permissions = NativeConvert.FromOctalPermissionString(mask);

                if ((permissions & ~FilePermissions.ACCESSPERMS) != 0)
                {
                    // Only allow access permissions
                    return false;
                }

                if ((permissions & FilePermissions.S_IRWXU) != FilePermissions.S_IRWXU)
                {
                    // We expect at least full owner permissions (700)
                    return false;
                }

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public override void CopyPermissions(string sourcePath, string targetPath)
        {
            try
            {
                Syscall.stat(sourcePath, out var srcStat);
                Syscall.stat(targetPath, out var tgtStat);

                if (srcStat.st_mode != tgtStat.st_mode)
                {
                    Syscall.chmod(targetPath, srcStat.st_mode);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Failed to copy permissions from {SourcePath} to {TargetPath}", sourcePath, targetPath);
            }
        }

        protected override List<IMount> GetAllMounts()
        {
            return _procMountProvider.GetMounts()
                                     .Concat(GetDriveInfoMounts()
                                                 .Select(d => new DriveInfoMount(d, FindDriveType.Find(d.DriveFormat)))
                                                 .Where(d => d.DriveType == DriveType.Fixed ||
                                                             d.DriveType == DriveType.Network ||
                                                             d.DriveType == DriveType.Removable))
                                     .DistinctBy(v => v.RootDirectory)
                                     .ToList();
        }

        protected override bool IsSpecialMount(IMount mount)
        {
            var root = mount.RootDirectory;

            if (root.StartsWith("/var/lib/"))
            {
                // Could be /var/lib/docker when docker uses zfs. Very unlikely that a useful mount is located in /var/lib.
                return true;
            }

            if (root.StartsWith("/snap/"))
            {
                // Mount point for snap packages
                return true;
            }

            return false;
        }

        public override long? GetTotalSize(string path)
        {
            Ensure.That(path, () => path).IsValidPath();

            var mount = GetMount(path);

            return mount?.TotalSize;
        }

        protected override void CloneFileInternal(string source, string destination, bool overwrite)
        {
            if (!File.Exists(destination) && !UnixFileSystemInfo.GetFileSystemEntry(source).IsSymbolicLink)
            {
                if (_createRefLink.TryCreateRefLink(source, destination))
                {
                    return;
                }
            }

            CopyFileInternal(source, destination, overwrite);
        }

        protected override void CopyFileInternal(string source, string destination, bool overwrite)
        {
            var sourceInfo = UnixFileSystemInfo.GetFileSystemEntry(source);

            if (sourceInfo.IsSymbolicLink)
            {
                var isSameDir = UnixPath.GetDirectoryName(source) == UnixPath.GetDirectoryName(destination);
                var symlinkInfo = (UnixSymbolicLinkInfo)sourceInfo;
                var symlinkPath = symlinkInfo.ContentsPath;

                var newFile = new UnixSymbolicLinkInfo(destination);

                if (FileExists(destination) && overwrite)
                {
                    DeleteFile(destination);
                }

                if (isSameDir)
                {
                    // We're in the same dir, so we can preserve relative symlinks.
                    newFile.CreateSymbolicLinkTo(symlinkInfo.ContentsPath);
                }
                else
                {
                    var fullPath = UnixPath.Combine(UnixPath.GetDirectoryName(source), symlinkPath);
                    newFile.CreateSymbolicLinkTo(fullPath);
                }
            }
            else if (PlatformInfo.GetVersion() > new Version(6, 0) && (!FileExists(destination) || overwrite))
            {
                TransferFilePatched(source, destination, overwrite, false);
            }
            else
            {
                base.CopyFileInternal(source, destination, overwrite);
            }
        }

        protected override void MoveFileInternal(string source, string destination)
        {
            var sourceInfo = UnixFileSystemInfo.GetFileSystemEntry(source);

            if (sourceInfo.IsSymbolicLink)
            {
                var isSameDir = UnixPath.GetDirectoryName(source) == UnixPath.GetDirectoryName(destination);
                var symlinkInfo = (UnixSymbolicLinkInfo)sourceInfo;
                var symlinkPath = symlinkInfo.ContentsPath;

                var newFile = new UnixSymbolicLinkInfo(destination);

                if (isSameDir)
                {
                    // We're in the same dir, so we can preserve relative symlinks.
                    newFile.CreateSymbolicLinkTo(symlinkInfo.ContentsPath);
                }
                else
                {
                    var fullPath = UnixPath.Combine(UnixPath.GetDirectoryName(source), symlinkPath);
                    newFile.CreateSymbolicLinkTo(fullPath);
                }

                try
                {
                    // Finally remove the original symlink.
                    symlinkInfo.Delete();
                }
                catch
                {
                    // Removing symlink failed, so rollback the new link and throw.
                    newFile.Delete();
                    throw;
                }
            }
            else if (PlatformInfo.GetVersion() > new Version(6, 0) && !FileExists(destination))
            {
                TransferFilePatched(source, destination, false, true);
            }
            else
            {
                base.MoveFileInternal(source, destination);
            }
        }

        private void TransferFilePatched(string source, string destination, bool overwrite, bool move)
        {
            // Mono 6.x throws errors if permissions or timestamps cannot be set
            // - In 6.0 it'll leave a full length file
            // - In 6.6 it'll leave a zero length file
            // Catch the exception and attempt to handle these edgecases

            // Mono 6.x till 6.10 doesn't properly try use rename first.
            if (move && PlatformInfo.GetVersion() < new Version(6, 10))
            {
                if (Syscall.lstat(source, out var sourcestat) == 0 &&
                    Syscall.lstat(destination, out var deststat) != 0 &&
                    Syscall.rename(source, destination) == 0)
                {
                    Logger.LogTrace("Moved '{Source}' -> '{Destination}' using Syscall.rename", source, destination);
                    return;
                }
            }

            try
            {
                if (move)
                {
                    base.MoveFileInternal(source, destination);
                }
                else
                {
                    base.CopyFileInternal(source, destination, overwrite);
                }
            }
            catch (UnauthorizedAccessException)
            {
                var srcInfo = new FileInfo(source);
                var dstInfo = new FileInfo(destination);
                var exists = dstInfo.Exists && srcInfo.Exists;

                if (exists && dstInfo.Length == 0 && srcInfo.Length != 0)
                {
                    // mono >=6.6 bug: zero length file since chmod happens at the start
                    Logger.LogDebug("Mono failed to {2} file likely due to known mono bug, attempting to {Action} directly. '{Source}' -> '{Destination}'", move ? "move" : "copy", source, destination);

                    try
                    {
                        Logger.LogTrace("Copying content from {Source} to {Destination} ({Length} bytes)", source, destination, srcInfo.Length);
                        using (var srcStream = new FileStream(source, FileMode.Open, FileAccess.Read))
                        using (var dstStream = new FileStream(destination, FileMode.Create, FileAccess.Write))
                        {
                            srcStream.CopyTo(dstStream);
                        }
                    }
                    catch
                    {
                        // If it fails again then bail
                        throw;
                    }
                }
                else if (exists && dstInfo.Length == srcInfo.Length)
                {
                    // mono 6.0, 6.4 bug: full length file since utime and chmod happens at the end
                    var action = move ? "move" : "copy";
                    Logger.LogDebug("Mono failed to {Action1} file likely due to known mono bug, attempting to {Action2} directly. '{Source}' -> '{Destination}'", action, action, source, destination);

                    // Check at least part of the file since UnauthorizedAccess can happen due to legitimate reasons too
                    var checkLength = (int)Math.Min(64 * 1024, dstInfo.Length);
                    if (checkLength > 0)
                    {
                        var srcData = new byte[checkLength];
                        var dstData = new byte[checkLength];

                        Logger.LogTrace("Check last {CheckLength} bytes from {Destination}", checkLength, destination);

                        using (var srcStream = new FileStream(source, FileMode.Open, FileAccess.Read))
                        using (var dstStream = new FileStream(destination, FileMode.Open, FileAccess.Read))
                        {
                            srcStream.Position = srcInfo.Length - checkLength;
                            dstStream.Position = dstInfo.Length - checkLength;

                            srcStream.Read(srcData, 0, checkLength);
                            dstStream.Read(dstData, 0, checkLength);
                        }

                        for (var i = 0; i < checkLength; i++)
                        {
                            if (srcData[i] != dstData[i])
                            {
                                // Files aren't the same, the UnauthorizedAccess was unrelated
                                Logger.LogTrace("Copy was incomplete, rethrowing original error");
                                throw;
                            }
                        }

                        Logger.LogTrace("Copy was complete, finishing {Action} operation", move ? "move" : "copy");
                    }
                }
                else
                {
                    // Unrecognized situation, the UnauthorizedAccess was unrelated
                    throw;
                }

                if (exists)
                {
                    try
                    {
                        dstInfo.LastWriteTimeUtc = srcInfo.LastWriteTimeUtc;
                    }
                    catch
                    {
                        Logger.LogDebug("Unable to change last modified date for {Destination}, skipping.", destination);
                    }

                    if (move)
                    {
                        Logger.LogTrace("Removing source file {Source}", source);
                        File.Delete(source);
                    }
                }
            }
        }

        public override bool TryRenameFile(string source, string destination)
        {
            return Syscall.rename(source, destination) == 0;
        }

        public override bool TryCreateHardLink(string source, string destination)
        {
            try
            {
                var fileInfo = UnixFileSystemInfo.GetFileSystemEntry(source);

                if (fileInfo.IsSymbolicLink) return false;

                fileInfo.CreateLink(destination);
                return true;
            }
            catch (UnixIOException ex)
            {
                if (ex.ErrorCode == Errno.EXDEV)
                {
                    Logger.LogTrace("Hardlink '{Source}' to '{Destination}' failed due to cross-device access.", source, destination);
                }
                else
                {
                    Logger.LogDebug(ex, "Hardlink '{Source}' to '{Destination}' failed.", source, destination);
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Hardlink '{Source}' to '{Destination}' failed.", source, destination);
                return false;
            }
        }

        public override bool TryCreateRefLink(string source, string destination)
        {
            return _createRefLink.TryCreateRefLink(source, destination);
        }

        private uint GetUserId(string user)
        {
            if (user.IsNullOrWhiteSpace())
            {
                return UNCHANGED_ID;
            }

            uint userId;

            if (uint.TryParse(user, out userId))
            {
                return userId;
            }

            var u = Syscall.getpwnam(user);

            if (u == null)
            {
                throw new LinuxPermissionsException("Unknown user: {0}", user);
            }

            return u.pw_uid;
        }

        private uint GetGroupId(string group)
        {
            if (group.IsNullOrWhiteSpace())
                return UNCHANGED_ID;

            if (uint.TryParse(group, out var groupId))
                return groupId;

            var g = Syscall.getgrnam(group);

            if (g == null)
                throw new LinuxPermissionsException("Unknown group: {0}", group);

            return g.gr_gid;
        }
    }
}
