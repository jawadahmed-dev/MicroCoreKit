using MicroCoreKit.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net;

namespace MicroCoreKit.Pipelines;

public class ExceptionHandlingMiddleware : IExceptionFilter
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "An unhandled exception occurred.");

        var (statusCode, messages) = MapExceptionToResult(context.Exception);
        var result = Result<object>.Failure(statusCode, messages);

        context.Result = new ObjectResult(new
        {
            StatusCode = result.StatusCode,
            IsSuccess = result.IsSuccess,
            Messages = result.Messages,
            Value = result.Value
        })
        {
            StatusCode = (int)result.StatusCode
        };

        context.ExceptionHandled = true;
    }

    private (HttpStatusCode StatusCode, List<string> Messages) MapExceptionToResult(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, new List<string> { "Authentication is required to access this resource." }),
            KeyNotFoundException => (HttpStatusCode.NotFound, new List<string> { "The requested resource was not found." }),
            ArgumentException => (HttpStatusCode.BadRequest, new List<string> { ex.Message }),
            _ => (HttpStatusCode.InternalServerError, new List<string> { "An unexpected error occurred on the server." })
        };
    }
}