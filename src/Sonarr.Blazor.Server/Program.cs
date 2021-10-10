﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation;

namespace Sonarr.Blazor.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var startupArgs = new StartupContext(args);
            try
            {
                NzbDroneLogger.Register(startupArgs, false, true);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("NLog Exception: " + ex.ToString());
                throw;
            }

            return Host
                .CreateDefaultBuilder(args)
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .ConfigureServices(services =>
                            services.AddSingleton<IStartupContext>(provider => new StartupContext(args)))
                        .UseDefaultServiceProvider(options => options.ValidateOnBuild = false);
                });
        }
    }
}
