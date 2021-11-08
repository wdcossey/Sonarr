using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using NzbDrone.Common.Serializer;
using NzbDrone.SignalR;
using Sonarr.Server.HostedServices;
using Sonarr.Server.Middleware;
using Sonarr.Server.ModelConventions;

namespace Sonarr.Server
{
    public class Startup
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
            => Configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            //TODO: Fix NLog integration!
            services.AddSingleton<NLog.Logger>(provider => NLog.LogManager.GetCurrentClassLogger());

            services.AddSonarr();

            services.AddResponseCompression();

            services
                .AddControllers(options => options.Conventions.Add(new SwaggerNamespaceConvention()))
                .AddApplicationPart(Assembly.Load(new AssemblyName("Sonarr.Http")))
                //.AddApplicationPart(Assembly.Load(new AssemblyName("Sonarr.Api")))
                //.AddApplicationPart(Assembly.LoadFrom("Sonarr.Api.dll"))
                .AddApplicationPart(Assembly.Load(new AssemblyName("Sonarr.Api.V3")));

            services
                .AddRazorPages()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                    //options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    //options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                    options.JsonSerializerOptions.Converters.Add(new JsonVersionConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonHttpUriConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonTimeSpanConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonTimeSpanNullableConverter());
                });

            services.AddResponseCaching();
            services.AddHostedService<SonarrHostedService>();
            services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.IgnoreNullValues = true;
                    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                    options.PayloadSerializerOptions.Converters.Add(new JsonVersionConverter());
                    options.PayloadSerializerOptions.Converters.Add(new JsonHttpUriConverter());
                    options.PayloadSerializerOptions.Converters.Add(new JsonTimeSpanConverter());
                    options.PayloadSerializerOptions.Converters.Add(new JsonTimeSpanNullableConverter());
                });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sonarr", Version = "v1" });
                c.SwaggerDoc("v3", new OpenApiInfo { Title = "Sonarr", Version = "v3" });
                c.SwaggerGeneratorOptions.ConflictingActionsResolver = enumerable => enumerable.First();
            });

            //services.AddAuthorization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler("/error");

            if (!env.IsDevelopment())
            {
                //app.UseHsts();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sonarr v1");
                c.SwaggerEndpoint("/swagger/v3/swagger.json", "Sonarr v3");
                c.RoutePrefix = "swagger";

            });

            app.UseSonarr();

            //app.UseHttpsRedirection();

            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), "UI")),
                RequestPath = string.Empty
            });

            app.UseRouting();

            //app.UseAuthentication();
            //app.UseAuthorization();

            app.UseMiddleware<SonarrResponseHeaderMiddleware>();
            //app.UseMiddleware<ApiKeyAuthorizationMiddleware>();

            app.UseResponseCaching();
            app.UseResponseCompression();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<SonarrHub>("/signalr");

                endpoints.MapRazorPages();
                endpoints.MapControllers()/*.RequireAuthorization()*/;
                endpoints.MapFallbackToFile("index.html");
            });
        }

    }
}
