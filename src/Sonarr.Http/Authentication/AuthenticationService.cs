using System;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using Sonarr.Http.Extensions;

namespace Sonarr.Http.Authentication
{
    public interface IAuthenticationService //: IUserValidator, IUserMapper
    {
        void SetContext(HttpContext context);

        void LogUnauthorized(HttpContext context);
        User Login(HttpContext context, string username, string password);
        void Logout(HttpContext context);
        bool IsAuthenticated(HttpContext context);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private const string AnonymousUser = "Anonymous";
        private readonly IUserService _userService;

        private const string HEADER_API_KEY_NAME = "X-Api-Key";
        private const string QUERY_API_KEY_NAME = "ApiKey";
        private static string _apiKey;
        private static AuthenticationType _authenticationMethod;

        [ThreadStatic]
        private static HttpContext _context;

        public AuthenticationService(
            ILogger<AuthenticationService> logger,
            IConfigFileProvider configFileProvider,
            IUserService userService)
        {
            _logger = logger;
            _userService = userService;
            _apiKey = configFileProvider.ApiKey;
            _authenticationMethod = configFileProvider.AuthenticationMethod;
        }

        public void SetContext(HttpContext context)
        {
            // Validate and GetUserIdentifier don't have access to the NancyContext so get it from the pipeline earlier
            _context = context;
        }

        public User Login(HttpContext context, string username, string password)
        {
            if (_authenticationMethod == AuthenticationType.None)
            {
                return null;
            }

            var user = _userService.FindUser(username, password);

            if (user != null)
            {
                LogSuccess(context, username);

                return user;
            }

            LogFailure(context, username);

            return null;
        }

        public void Logout(HttpContext context)
        {
            if (_authenticationMethod == AuthenticationType.None)
                return;

            if (context.User != null)
                LogLogout(context, context.User.Identity.Name);
        }

        public ClaimsPrincipal Validate(string username, string password)
        {
            if (_authenticationMethod == AuthenticationType.None)
                return new ClaimsPrincipal(new GenericIdentity(AnonymousUser));

            var user = _userService.FindUser(username, password);

            if (user != null)
            {
                if (_authenticationMethod != AuthenticationType.Basic)
                {
                    // Don't log success for basic auth
                    LogSuccess(_context, username);
                }

                return new ClaimsPrincipal(new GenericIdentity(user.Username));
            }

            LogFailure(_context, username);

            return null;
        }

        public ClaimsPrincipal GetUserFromIdentifier(Guid identifier, HttpContext context)
        {
            if (_authenticationMethod == AuthenticationType.None)
                return new ClaimsPrincipal(new GenericIdentity(AnonymousUser));

            var user = _userService.FindUser(identifier);

            if (user != null)
                return new ClaimsPrincipal(new GenericIdentity(user.Username));

            LogInvalidated(_context);

            return null;
        }

        public bool IsAuthenticated(HttpContext context)
        {
            var apiKey = GetApiKey(context);

            if (context.Request.IsApiRequest())
            {
                return ValidApiKey(apiKey);
            }

            if (_authenticationMethod == AuthenticationType.None)
            {
                return true;
            }

            if (context.Request.IsFeedRequest())
            {
                if (ValidUser(context) || ValidApiKey(apiKey))
                {
                    return true;
                }

                return false;
            }

            if (context.Request.IsLoginRequest())
            {
                return true;
            }

            if (context.Request.IsContentRequest())
            {
                return true;
            }

            if (context.Request.IsBundledJsRequest())
            {
                return true;
            }
            
            if (context.Request.IsPingRequest())
            {
                return true;
            }

            if (ValidUser(context))
            {
                return true;
            }

            return false;
        }

        private bool ValidUser(HttpContext context)
        {
            if (context.User != null) return true;

            return false;
        }

        private bool ValidApiKey(string apiKey)
        {
            if (_apiKey.Equals(apiKey)) return true;

            return false;
        }

        private string GetApiKey(HttpContext context)
        {
            context.Request.Headers.TryGetValue(HEADER_API_KEY_NAME, out var apiKeyHeader);

            if (!string.IsNullOrWhiteSpace(apiKeyHeader))
                return apiKeyHeader;

            context.Request.Query.TryGetValue(QUERY_API_KEY_NAME, out var apiKeyQueryString);

            if (!string.IsNullOrWhiteSpace(apiKeyQueryString))
                return apiKeyQueryString;

            return null;//context.Request.Headers.Authorization;
        }

        public void LogUnauthorized(HttpContext context)
            => _logger.LogInformation("Auth-Unauthorized ip {RemoteIpAddress} url '{Path}'", context.Connection.RemoteIpAddress.MapToIPv4(), context.Request.Path);

        private void LogInvalidated(HttpContext context)
            => _logger.LogInformation("Auth-Invalidated ip {RemoteIpAddress}", context.Connection.RemoteIpAddress.MapToIPv4());

        private void LogFailure(HttpContext context, string username)
            => _logger.LogWarning("Auth-Failure ip {RemoteIpAddress} username '{Username}'", context.Connection.RemoteIpAddress.MapToIPv4(), username);

        private void LogSuccess(HttpContext context, string username)
            => _logger.LogInformation("Auth-Success ip {RemoteIpAddress} username '{Username}'", context.Connection.RemoteIpAddress.MapToIPv4(), username);

        private void LogLogout(HttpContext context, string username)
            => _logger.LogInformation("Auth-Logout ip {RemoteIpAddress} username '{Username}'", context.Connection.RemoteIpAddress.MapToIPv4(), username);

    }
}
