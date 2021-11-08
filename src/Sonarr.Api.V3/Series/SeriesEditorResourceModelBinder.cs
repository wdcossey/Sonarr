using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NzbDrone.Common.Serializer;

namespace Sonarr.Api.V3.Series
{
    public class SeriesEditorResourceModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            try
            {
                //Json encoded data sent as `application/x-www-form-urlencoded`, grab it from the Form Key
                var resource = Json.Deserialize<SeriesEditorResource>(bindingContext.HttpContext.Request.Form.Keys.First());
                bindingContext.Result = ModelBindingResult.Success(resource);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
