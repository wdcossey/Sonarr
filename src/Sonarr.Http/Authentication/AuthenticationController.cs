using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;

namespace Sonarr.Http.Authentication
{
    [AllowAnonymous]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IConfigFileProvider _configFileProvider;

        public AuthenticationController(IAuthenticationService authService, IConfigFileProvider configFileProvider)
        {
            _authService = authService;
            _configFileProvider = configFileProvider;
        }

        [Route("/login")]
        [HttpPost]
        public IActionResult Login(
            [FromForm] [ModelBinder(typeof(LoginResourceModelBinder))] LoginResource resource,
            [FromQuery] string returnUrl)
        {
            var user = _authService.Login(HttpContext, resource.Username, resource.Password);

            if (user == null)
                return Redirect($"~/login?returnUrl={returnUrl}&loginFailed=true");

            DateTime? expiry = null;

            if (resource.RememberMe)
            {
                expiry = DateTime.UtcNow.AddDays(7);
            }

            return Redirect(_configFileProvider.UrlBase + "/");
            //return this.LoginAndRedirect(user.Identifier, expiry, _configFileProvider.UrlBase + "/");
        }

        [Route("/logout")]
        [HttpPost]
        public IActionResult Logout()
        {
            _authService.Logout(HttpContext);

            return Redirect(_configFileProvider.UrlBase + "/");
            //return this.LogoutAndRedirect(_configFileProvider.UrlBase + "/");
        }
    }
}
