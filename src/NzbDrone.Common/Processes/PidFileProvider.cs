using System;
using System.IO;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Common.Processes
{
    public interface IProvidePidFile
    {
        void Write();
    }

    public class PidFileProvider : IProvidePidFile
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IProcessProvider _processProvider;
        private readonly ILogger<PidFileProvider> _logger;

        public PidFileProvider(ILogger<PidFileProvider> logger, IAppFolderInfo appFolderInfo, IProcessProvider processProvider)
        {
            _logger = logger;
            _appFolderInfo = appFolderInfo;
            _processProvider = processProvider;
        }

        public void Write()
        {
            if (OsInfo.IsWindows)
            {
                return;
            }

            var filename = Path.Combine(_appFolderInfo.AppDataFolder, "sonarr.pid");
            try
            {
                File.WriteAllText(filename, _processProvider.GetCurrentProcessId().ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to write PID file {FileName}", filename);
                throw new SonarrStartupException(ex, "Unable to write PID file {0}", filename);
            }
        }
    }
}
