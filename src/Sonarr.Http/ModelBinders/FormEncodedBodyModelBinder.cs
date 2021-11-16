using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NzbDrone.Common.Serializer;

namespace Sonarr.Http.ModelBinders
{
    /// <summary>
    /// The Sonarr frontend posts some Ajax data as `application/x-www-form-urlencoded`, funky!
    /// </summary>
    public class FormEncodedBodyModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            try
            {
                // Seems that the content-type is `application/x-www-form-urlencoded`, can't use `[FromBodyAttribute]`
                // The json data is in the first key of the Request Form.
                var resource = Json.Deserialize(bindingContext.HttpContext.Request.Form.Keys.First(), bindingContext.ModelType);
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
