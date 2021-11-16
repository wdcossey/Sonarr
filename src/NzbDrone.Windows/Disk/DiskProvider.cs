using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;

namespace NzbDrone.Windows.Disk
{
    public class DiskProvider : DiskProviderBase
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        public DiskProvider(ILogger<DiskProvider> logger)
            : base(logger) { }

        public override long? GetAvailableSpace(string path)
        {
            Ensure.That(path, () => path).IsValidPath();

            var root = GetPathRoot(path);

            if (!FolderExists(root))
                throw new DirectoryNotFoundException(root);

            return DriveFreeSpaceEx(root);
        }

        public override void InheritFolderPermissions(string filename)
        {
            Ensure.That(filename, () => filename).IsValidPath();

            var fileInfo = new FileInfo(filename);
            var fs = fileInfo.GetAccessControl(AccessControlSections.Access);
            fs.SetAccessRuleProtection(false, false);
            fileInfo.SetAccessControl(fs);
        }

        public override void SetEveryonePermissions(string filename)
        {
            var accountSid = WellKnownSidType.WorldSid;
            var rights = FileSystemRights.Modify;
            var controlType = AccessControlType.Allow;

            try
            {
                var sid = new SecurityIdentifier(accountSid, null);

                var directoryInfo = new DirectoryInfo(filename);
                var directorySecurity = directoryInfo.GetAccessControl(AccessControlSections.Access);

                var rules = directorySecurity.GetAccessRules(true, false, typeof(SecurityIdentifier));

                if (rules.OfType<FileSystemAccessRule>().Any(acl => acl.AccessControlType == controlType && (acl.FileSystemRights & rights) == rights && acl.IdentityReference.Equals(sid)))
                {
                    return;
                }

                var accessRule = new FileSystemAccessRule(sid, rights,
                                                          InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                                          PropagationFlags.InheritOnly, controlType);

                bool modified;
                directorySecurity.ModifyAccessRule(AccessControlModification.Add, accessRule, out modified);

                if (modified)
                {
                    directoryInfo.SetAccessControl(directorySecurity);
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Couldn't set permission for {Filename}. account:{AccountSid} rights:{Rights} accessControlType:{ControlType}", filename, accountSid, rights, controlType);
                throw;
            }

        }

        public override void SetFilePermissions(string path, string mask, string group)
        {

        }

        public override void SetPermissions(string path, string mask, string group)
        {

        }

        public override void CopyPermissions(string sourcePath, string targetPath)
        {

        }

        public override long? GetTotalSize(string path)
        {
            Ensure.That(path, () => path).IsValidPath();

            var root = GetPathRoot(path);

            if (!FolderExists(root))
                throw new DirectoryNotFoundException(root);

            return DriveTotalSizeEx(root);
        }

        private static long DriveFreeSpaceEx(string folderName)
        {
            Ensure.That(folderName, () => folderName).IsValidPath();

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }

            ulong free = 0;
            ulong dummy1 = 0;
            ulong dummy2 = 0;

            if (GetDiskFreeSpaceEx(folderName, out free, out dummy1, out dummy2))
            {
                return (long)free;
            }

            return 0;
        }

        private static long DriveTotalSizeEx(string folderName)
        {
            Ensure.That(folderName, () => folderName).IsValidPath();

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }

            ulong total = 0;
            ulong dummy1 = 0;
            ulong dummy2 = 0;

            if (GetDiskFreeSpaceEx(folderName, out dummy1, out total, out dummy2))
            {
                return (long)total;
            }

            return 0;
        }


        public override bool TryCreateHardLink(string source, string destination)
        {
            try
            {
                return CreateHardLink(destination, source, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Hardlink '{Source}' to '{Destination}' failed.", source, destination);
                return false;
            }
        }
    }
}
