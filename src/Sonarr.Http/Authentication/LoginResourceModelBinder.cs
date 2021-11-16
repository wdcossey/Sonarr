using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Sonarr.Http.Authentication
{
    public class LoginResourceModelBinder: IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            try
            {
                if (!typeof(LoginResource).IsAssignableFrom(bindingContext.ModelType))
                    throw new InvalidOperationException($"{bindingContext.ModelType} is not supported by {typeof(LoginResourceModelBinder)}");

                var loginResource = new LoginResource
                {
                    Username = bindingContext.ValueProvider.GetValue("username").FirstValue,
                    Password = bindingContext.ValueProvider.GetValue("password").FirstValue,
                    RememberMe = bindingContext.ValueProvider.GetValue("rememberMe").FirstValue?.Equals("on", StringComparison.OrdinalIgnoreCase) == true
                };

                bindingContext.Result = ModelBindingResult.Success(loginResource);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
