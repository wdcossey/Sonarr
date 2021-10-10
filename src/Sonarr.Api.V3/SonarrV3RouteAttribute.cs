using System;
using Microsoft.AspNetCore.Mvc;

namespace Sonarr.Api.V3
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class SonarrV3RouteAttribute : RouteAttribute
    {
        public SonarrV3RouteAttribute(string template)
            : base($"/api/v3/{template}") { }
    }
}
