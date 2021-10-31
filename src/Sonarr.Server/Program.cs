using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Web;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Configuration;

namespace Sonarr.Server
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            var startupContext = new StartupContext(args);
            try
            {
                Console.WriteLine($"OsInfo.Os: {OsInfo.Os}");
                Console.WriteLine($"OsInfo.Os: {OsInfo.Os}");
                Console.WriteLine($"Environment.OSVersion: {Environment.OSVersion}");
                Console.WriteLine($"Environment.OSVersion.Version: {Environment.OSVersion.Version}");
                Console.WriteLine($"Environment.OSVersion.VersionString: {Environment.OSVersion.VersionString}");
                Console.WriteLine($"RuntimeInformation.OSDescription: {RuntimeInformation.OSDescription}");
                Console.WriteLine($"RuntimeInformation.OSArchitecture: {RuntimeInformation.OSArchitecture}");

                NzbDroneLogger.Register(startupContext, false, true);
                return CreateHostBuilder(startupContext, args).Build().RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("NLog Exception: " + ex.ToString());
                throw;
            }
        }

        public static IHostBuilder CreateHostBuilder(IStartupContext startupContext, string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseWebRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI")) //TODO: Should be configurable
                        .UseStartup<Startup>()
                        .UseKestrel(options =>
                        {
                            var sonarrConfig = options.ApplicationServices.GetRequiredService<IConfigFileProvider>();
                            options.AddServerHeader = false;
                            //options.Listen(IPAddress.Parse(sonarrConfig.BindAddress), sonarrConfig.SslPort);
                            options.ListenAnyIP(sonarrConfig.SslPort, listenOptions =>
                            {
                                listenOptions.UseHttps();
                            });
                        })
                        .UseKestrel(options =>
                        {
                            var sonarrConfig = options.ApplicationServices.GetRequiredService<IConfigFileProvider>();
                            options.AddServerHeader = false;
                            //options.Listen(IPAddress.Parse(sonarrConfig.BindAddress), sonarrConfig.Port);
                            options.ListenAnyIP(sonarrConfig.Port);
                        })
                        .ConfigureServices(services => services.AddSingleton(_ => startupContext))
                        .UseDefaultServiceProvider(options => options.ValidateOnBuild = false);
                });
        }
    }
}
