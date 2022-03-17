using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Web;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Configuration;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Sonarr.Server
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            var startupContext = new StartupContext(args);

            var logger = NLog.LogManager.GetLogger(nameof(Program));

            try
            {
                NzbDroneLogger.Register(startupContext, false, true);
                return CreateHostBuilder(startupContext, args).Build().RunAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "NLog Exception: {Exception}", ex);
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
                            options.ListenAnyIP(sonarrConfig.SslPort, listenOptions => { listenOptions.UseHttps(); });
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
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .UseNLog();
        }
    }
}
