using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Nancy;
using Sonarr.Blazor.Server.Hubs;

namespace Sonarr.Blazor.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //TODO: Fix NLog integration!
            services.AddSingleton<NLog.Logger>(provider => NLog.LogManager.GetCurrentClassLogger());

            services.AddSonarr();

            services
                .AddControllersWithViews()
                .AddApplicationPart(Assembly.Load(new AssemblyName("Sonarr.Api.V3")));

            services
                .AddRazorPages()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                    //options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                });

            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseWebSockets();
            app.UseSonarr();

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), "UI")),
                RequestPath = ""
            });


            app.UseRouting();

            /*app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/signalr/negotiate")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
                        {
                            //await Echo(context, webSocket);
                        }
                    }
                    else
                    {
                        //
                        context.Response.ContentType = MediaTypeNames.Application.Json;
                        await context.Response.WriteAsync(
                            "{\"Url\":\"/signalr\",\"ConnectionToken\":\"AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAAr8gpH6UcEk6IKdqp5Unh8gAAAAACAAAAAAAQZgAAAAEAACAAAADC/ufNckUv84DJFUlYRquB+BsuUNhFefuGRFt/3knHWAAAAAAOgAAAAAIAACAAAACgIL69hHBlajFcQB+dHhcU+fk+sbl9b0XYlamrSsMd+DAAAABdn+rvqWOAsNrazV3XoPKoUgZJctwWW6Hun0ICix4lPNSnVDxmglYsOgIsq2/LbBxAAAAAFlHl48zQUChCr+AjY+kxIbcEN410xworVQjvem2i1A3Wjq10R+w5TjP8mGX6kXllLa5dkg45xCVHcJZt8+fAiw==\",\"ConnectionId\":\"d2b62fe8-b9bb-47db-af03-990256176e69\",\"KeepAliveTimeout\":60.0,\"DisconnectTimeout\":180.0,\"ConnectionTimeout\":110.0,\"TryWebSockets\":true,\"ProtocolVersion\":\"2.0\",\"TransportConnectTimeout\":5.0,\"LongPollDelay\":0.0}", Encoding.UTF8);

                        context.Response.StatusCode = (int) HttpStatusCode.OK;
                    }
                }
                else
                {
                    await next();
                }

            });*/

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<SonarrHub>("/signalr");
                endpoints.MapControllerRoute(name: "initialize", pattern: "{controller=Initialize}/initialize.js");

                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }

    }
}
