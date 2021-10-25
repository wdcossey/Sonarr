using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Messaging.Commands;

namespace Sonarr.Api.V3.Commands
{
    public class CommandModelBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            try
            {
                bindingContext.HttpContext.Request.EnableBuffering();
                bindingContext.HttpContext.Request.Body.Position = 0;

                var resource = await Json.DeserializeAsync<CommandResource>(bindingContext.HttpContext.Request.Body);

                var commandFactory = bindingContext.HttpContext.RequestServices.GetRequiredService<ICommandFactory>();
                var command = await commandFactory.CreateAsync(resource.Name, bindingContext.HttpContext.Request.Body);

                command!.Trigger = CommandTrigger.Manual;
                command!.SuppressMessages = !resource!.SendUpdatesToClient;
                command!.SendUpdatesToClient = true;

                if (bindingContext.HttpContext.Request.Headers.TryGetValue("User-Agent", out var userAgent))
                    command!.ClientUserAgent = userAgent;

                bindingContext.Result = ModelBindingResult.Success(command);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex.Message);
            }

        }
    }
}
