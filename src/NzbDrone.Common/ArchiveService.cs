using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;

namespace NzbDrone.Common
{
    public interface IArchiveService
    {
        void Extract(string compressedFile, string destination);
        void CreateZip(string path, params string[] files);
    }

    public class ArchiveService : IArchiveService
    {
        private readonly ILogger<ArchiveService> _logger;

        public ArchiveService(ILogger<ArchiveService> logger)
        {
            _logger = logger;
        }

        public void Extract(string compressedFile, string destination)
        {
            _logger.LogDebug("Extracting archive [{CompressedFile}] to [{Destination}]", compressedFile, destination);

            if (compressedFile.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                ExtractZip(compressedFile, destination);
            }
            else
            {
                ExtractTgz(compressedFile, destination);
            }

            _logger.LogDebug("Extraction complete.");
        }

        public void CreateZip(string path, params string[] files)
        {
            using (var zipFile = ZipFile.Create(path))
            {
                zipFile.BeginUpdate();

                foreach (var file in files)
                {
                    zipFile.Add(file, Path.GetFileName(file));
                }

                zipFile.CommitUpdate();
            }
        }

        private void ExtractZip(string compressedFile, string destination)
        {
            using (var fileStream = File.OpenRead(compressedFile))
            {
                var zipFile = new ZipFile(fileStream);

                _logger.LogDebug("Validating Archive {CompressedFile}", compressedFile);

                if (!zipFile.TestArchive(true, TestStrategy.FindFirstError, OnZipError))
                {
                    throw new IOException(string.Format("File {0} failed archive validation.", compressedFile));
                }

                foreach (ZipEntry zipEntry in zipFile)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue; // Ignore directories
                    }
                    string entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    byte[] buffer = new byte[4096]; // 4K is optimum
                    Stream zipStream = zipFile.GetInputStream(zipEntry);

                    // Manipulate the output filename here as desired.
                    string fullZipToPath = Path.Combine(destination, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }
            }
        }

        private void ExtractTgz(string compressedFile, string destination)
        {
            Stream inStream = File.OpenRead(compressedFile);
            Stream gzipStream = new GZipInputStream(inStream);

            TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
            tarArchive.ExtractContents(destination);
            tarArchive.Close();

            gzipStream.Close();
            inStream.Close();
        }

        private void OnZipError(TestStatus status, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _logger.LogError("File {FileName} failed zip validation. {Message}", status.File.Name, message);
            }
        }
    }
}
