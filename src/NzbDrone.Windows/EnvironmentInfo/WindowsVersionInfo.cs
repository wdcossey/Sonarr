using System;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Windows.EnvironmentInfo
{
    public class WindowsVersionInfo : IOsVersionAdapter
    {
        private readonly ILogger<WindowsVersionInfo> _logger;
        public bool Enabled => OsInfo.IsWindows;

        public WindowsVersionInfo(ILogger<WindowsVersionInfo> logger)
            => _logger = logger;

        public OsVersionModel Read()
        {
            var windowsServer = IsServer();
            var osName = windowsServer ? "Windows Server" : "Windows";
            return new OsVersionModel(osName, Environment.OSVersion.Version.ToString(), Environment.OSVersion.VersionString);
        }

        private bool IsServer()
        {
            try
            {
                const string subkey = @"Software\Microsoft\Windows NT\CurrentVersion";
                var openSubKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey);
                if (openSubKey != null)
                {
                    var productName = openSubKey.GetValue("ProductName").ToString();

                    if (productName.ToLower().Contains("server"))
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Couldn't detect if running Windows Server");
            }

            return false;
        }
    }
}