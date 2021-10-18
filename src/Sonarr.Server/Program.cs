using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Web;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation;

namespace Sonarr.Server
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            var startupContext = new StartupContext(args);
            try
            {
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
                        .UseWebRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI"))
                        .UseStartup<Startup>()
                        .UseKestrel(options => options.AddServerHeader = false)
                        .ConfigureServices(services => services.AddSingleton(_ => startupContext))
                        .UseDefaultServiceProvider(options => options.ValidateOnBuild = false);
                });
        }
    }
}
