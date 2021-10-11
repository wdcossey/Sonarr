using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;

namespace Sonarr.Api.V3
{
    //TODO: Move this to Http

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class SonarrApiRouteAttribute : RouteAttribute
    {
        public SonarrApiRouteAttribute(string template, RouteVersion version)
            : base($"/api{(version == RouteVersion.V1 ? "" : $"/v{(char)version}")}/{template}") { }
    }

    /*[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class SonarrApiV3RouteAttribute : SonarrApiRouteAttribute
    {
        public SonarrApiV3RouteAttribute(string template)
            : base(template, RouteVersion.V3) { }
    }*/

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class SonarrFeedRouteAttribute : RouteAttribute
    {
        public SonarrFeedRouteAttribute(string template, RouteVersion version)
            : base($"/feed{(version == RouteVersion.V1 ? "" : $"/v{(char)version}")}/{template}") { }
    }

    /*[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class SonarrFeedV3RouteAttribute : SonarrFeedRouteAttribute
    {
        public SonarrFeedV3RouteAttribute(string template, RouteVersion version)
            : base(template, version) { }
    }*/

    //TODO: Move!
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class SonarrApiConfigRouteAttribute : SonarrApiRouteAttribute
    {
        public SonarrApiConfigRouteAttribute(string template, RouteVersion version)
            : base($"config/{template}", version)
        {
        }
    }

    public enum RouteVersion
    {
        V1 = '\0',
        V3 = '3'
    }
}
