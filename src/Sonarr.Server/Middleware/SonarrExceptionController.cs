using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download.Clients.Nzbget;
using NzbDrone.Core.Exceptions;

namespace Sonarr.Server.Middleware
{
    [ApiController]
    public class SonarrExceptionController: ControllerBase
    {
        private readonly ILogger<SonarrExceptionController> _logger;

        public SonarrExceptionController(ILogger<SonarrExceptionController> logger)
        {
            _logger = logger;
        }

        // URL for this API - /api/error
        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("/error")]
        [Route("/api/error")]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature =
                HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            var exception = exceptionHandlerPathFeature.Error;

            //if (exception is ApiException apiException)
            //{
            //    _logger.Warn(apiException, "API Error");
            //    return apiException.ToErrorResponse(context);
            //}

            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate, max-age=0");
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "0");

            if (exception is ValidationException validationException)
            {
                _logger.LogWarning("Invalid request {Message}", validationException.Message);
                return BadRequest(validationException.Errors);

                /*
                _logger.Warn("Invalid request {0}", validationException.Message);
                return validationException.Errors.AsResponse(context, HttpStatusCode.BadRequest);
                 */
            }

            if (exception is NzbDroneClientException clientException)
            {
                return StatusCode((int)clientException.StatusCode,
                    new { Message = exception.Message, Description = exception.ToString() });
                /*
                return new ErrorModel
                {
                    Message = exception.Message,
                    Description = exception.ToString()
                }.AsResponse(context, (HttpStatusCode)clientException.StatusCode);
                 */
            }

            if (exception is ModelNotFoundException notFoundException)
            {
                return NotFound(new { Message = exception.Message, Description = exception.ToString() } );
                /*return new ErrorModel
                {
                    Message = exception.Message,
                    Description = exception.ToString()
                }.AsResponse(context, HttpStatusCode.NotFound);*/
            }

            if (exception is ModelConflictException conflictException)
            {
                return Conflict(new { Message = exception.Message, Description = exception.ToString() } );

                /*
                return new ErrorModel
                {
                    Message = exception.Message,
                    Description = exception.ToString()
                }.AsResponse(context, HttpStatusCode.Conflict);
                */

            }

            if (exception is SQLiteException sqLiteException)
            {
                if (Request.Method == HttpMethods.Put || Request.Method == HttpMethods.Post)
                {
                    if (sqLiteException.Message.Contains("constraint failed"))
                        return Conflict(new { Message = exception.Message } );

                        /*return new ErrorModel
                        {
                            Message = exception.Message,
                        }.AsResponse(context, HttpStatusCode.Conflict);*/
                }

                _logger.LogError(sqLiteException, "[{Method} {Path}]", Request.Method, Request.Path);
                //_logger.Error(sqLiteException, "[{0} {1}]", context.Request.Method, context.Request.Path);
            }

            _logger.LogCritical(exception, "Request Failed. {Method} {Path}", Request.Method, Request.Path);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = exception.Message, Description = exception.ToString() });

            /*return new ErrorModel
            {
                Message = exception.Message,
                Description = exception.ToString()
            }.AsResponse(context, HttpStatusCode.InternalServerError);*/

            return Problem();
        }
    }
}
