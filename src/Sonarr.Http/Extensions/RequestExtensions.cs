using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using NzbDrone.Common.Extensions;

namespace Sonarr.Http.Extensions
{
    public static class RequestExtensions
    {
        public static bool IsApiRequest(this HttpRequest request)
            => request.Path.StartsWithSegments("/api", StringComparison.InvariantCultureIgnoreCase);

        public static bool IsFeedRequest(this HttpRequest request)
            => request.Path.StartsWithSegments("/feed", StringComparison.InvariantCultureIgnoreCase);
        
        public static bool IsPingRequest(this HttpRequest request)
            => request.Path.StartsWithSegments("/ping", StringComparison.InvariantCultureIgnoreCase);

        public static bool IsSignalRRequest(this HttpRequest request)
            => request.Path.StartsWithSegments("/hubs", StringComparison.InvariantCultureIgnoreCase);
        
        public static bool IsLocalRequest(this HttpRequest request)
        {
            var remoteIpAddress = request.HttpContext.Connection.RemoteIpAddress?.ToString();

            return remoteIpAddress?.Equals("localhost") == true ||
                   remoteIpAddress?.Equals("127.0.0.1") == true ||
                   remoteIpAddress?.Equals("::1") == true;
        }

        /*public static bool IsLoginRequest(this Request request)
        {
            return request.Path.Equals("/login", StringComparison.InvariantCultureIgnoreCase);
        }*/

        public static bool IsLoginRequest(this HttpRequest request)
            => request.Path.Equals("/login", StringComparison.InvariantCultureIgnoreCase);

        public static bool IsContentRequest(this HttpRequest request)
            => request.Path.StartsWithSegments("/Content", StringComparison.InvariantCultureIgnoreCase);

        public static bool IsBundledJsRequest(this HttpRequest request)
            => !request.Path.Equals("/initialize.js", StringComparison.InvariantCultureIgnoreCase) &&
               request.Path.Value.EndsWith(".js", StringComparison.InvariantCultureIgnoreCase);

        public static bool IsSharedContentRequest(this HttpRequest request)
        {
            return request.Path.StartsWithSegments("/MediaCover", StringComparison.InvariantCultureIgnoreCase) ||
                   request.Path.StartsWithSegments("/Content/Images", StringComparison.InvariantCultureIgnoreCase);
        }
        

        /*public static bool GetBooleanQueryParameter(this Request request, string parameter, bool defaultValue = false)
        {
            var parameterValue = request.Query[parameter];

            if (parameterValue.HasValue)
            {
                return bool.Parse(parameterValue.Value);
            }

            return defaultValue;
        }*/

        /*public static int GetIntegerQueryParameter(this Request request, string parameter, int defaultValue = 0)
        {
            var parameterValue = request.Query[parameter];

            if (parameterValue.HasValue)
            {
                return int.Parse(parameterValue.Value);
            }

            return defaultValue;
        }*/

        /*public static int? GetNullableIntegerQueryParameter(this Request request, string parameter, int? defaultValue = null)
        {
            var parameterValue = request.Query[parameter];

            if (parameterValue.HasValue)
            {
                return int.Parse(parameterValue.Value);
            }

            return defaultValue;
        }*/

        public static string GetRemoteIp(this HttpContext context)
        {
            if (context?.Request == null || context.Connection.RemoteIpAddress == null)
                return "Unknown";

            var remoteAddress = context.Connection.RemoteIpAddress.ToString();
            IPAddress remoteIp;

            // Only check if forwarded by a local network reverse proxy
            if (IPAddress.TryParse(remoteAddress, out remoteIp) && remoteIp.IsLocalAddress())
            {
                var realIpHeader = context.Request.Headers["X-Real-IP"];
                if (realIpHeader.Any())
                    return realIpHeader.First();

                var forwardedForHeader = context.Request.Headers["X-Forwarded-For"];
                if (forwardedForHeader.Any())
                {
                    // Get the first address that was forwarded by a local IP to prevent remote clients faking another proxy
                    foreach (var forwardedForAddress in forwardedForHeader.SelectMany(v => v.Split(',')).Select(v => v.Trim()).Reverse())
                    {
                        if (!IPAddress.TryParse(forwardedForAddress, out remoteIp))
                            return remoteAddress;

                        if (!remoteIp.IsLocalAddress())
                            return forwardedForAddress;

                        remoteAddress = forwardedForAddress;
                    }
                }
            }

            return remoteAddress;
        }
    }
}
