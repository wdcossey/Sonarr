using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Sonarr.Server.ModelConventions
{
    public class SwaggerNamespaceConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            var controllerNamespace = controller.ControllerType.Namespace;
            var apiVersion = "v1";
            if (controllerNamespace?.Split('.').Any(a => a.Equals("V3", StringComparison.OrdinalIgnoreCase)) == true)
            {
                apiVersion = "v3";
            }

            controller.ApiExplorer.GroupName = apiVersion;
        }
    }
}
