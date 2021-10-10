using System;

namespace Sonarr.Api.V3.Config
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class SonarrV3ConfigRouteAttribute : SonarrV3RouteAttribute
    {
        public SonarrV3ConfigRouteAttribute(string template)
            : base($"config/{template}")
        {
        }
    }
}
