using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NzbDrone.Common.EnvironmentInfo
{

    public interface IPlatformInfo
    {
        Version Version { get; }
    }

    public class PlatformInfo : IPlatformInfo
    {
        private static readonly Version _version;

        static PlatformInfo()
        {
            _version = GetDotNetVersion();
        }

        public static string PlatformName => ".Net";

        public Version Version => _version;

        public static Version GetVersion() => _version;

        private static Version GetDotNetVersion()
        {
            try
            {
                return System.Environment.Version;
                /*const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
                {
                    if (ndpKey == null)
                    {
                        return new Version(4, 0);
                    }

                    var releaseKey = (int)ndpKey.GetValue("Release");

                    if (releaseKey >= 528040)
                    {
                        return new Version(4, 8, 0);
                    }
                    if (releaseKey >= 461808)
                    {
                        return new Version(4, 7, 2);
                    }
                    if (releaseKey >= 461308)
                    {
                        return new Version(4, 7, 1);
                    }
                    if (releaseKey >= 460798)
                    {
                        return new Version(4, 7);
                    }
                    if (releaseKey >= 394802)
                    {
                        return new Version(4, 6, 2);
                    }
                    if (releaseKey >= 394254)
                    {
                        return new Version(4, 6, 1);
                    }
                    if (releaseKey >= 393295)
                    {
                        return new Version(4, 6);
                    }
                    if (releaseKey >= 379893)
                    {
                        return new Version(4, 5, 2);
                    }
                    if (releaseKey >= 378675)
                    {
                        return new Version(4, 5, 1);
                    }
                    if (releaseKey >= 378389)
                    {
                        return new Version(4, 5);
                    }
                }*/
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Couldn't get .NET Runtime version: {ex}");
            }

            return new Version(4, 0);
        }
    }
}
