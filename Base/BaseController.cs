using MediatR;
using MicroCoreKit.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web.Http;

namespace MicroCoreKit.Base;

[ApiController]
public abstract class BaseController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BaseController> _logger;

    protected BaseController(IMediator mediator, ILogger<BaseController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a MediatR request and returns the result as an IActionResult.
    /// </summary>
    protected async Task<IActionResult> SendRequest<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<Result<TResponse>>
    {
        try
        {
            _logger.LogInformation("Processing request: {RequestType}", typeof(TRequest).Name);
            var result = await _mediator.Send(request, cancellationToken);

            if (result.IsSuccess)
            {
                return StatusCode((int)result.StatusCode, result.Value);
            }
            else
            {
                return StatusCode((int)result.StatusCode, new { Messages = result.Messages });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request: {RequestType}", typeof(TRequest).Name);
            return StatusCode((int)HttpStatusCode.InternalServerError, new { Messages = new List<string> { "An unexpected error occurred." } });
        }
    }

    /// <summary>
    /// Returns a success result with the specified value and optional message.
    /// </summary>
    protected IActionResult OkResult<T>(T value, string message = null)
    {
        var result = Result<T>.Success(value, HttpStatusCode.OK, message);
        return StatusCode((int)result.StatusCode, value);
    }

    /// <summary>
    /// Returns a failure result with the specified status code and messages.
    /// </summary>
    protected IActionResult ErrorResult(HttpStatusCode statusCode, IEnumerable<string> messages)
    {
        var result = Result<object>.Failure(statusCode, messages);
        return StatusCode((int)result.StatusCode, new { Messages = result.Messages });
    }

    /// <summary>
    /// Checks if the current user is authorized for a specific role or claim.
    /// </summary>
    protected bool IsAuthorized(string roleOrClaim)
    {
        return User.IsInRole(roleOrClaim) || User.HasClaim(c => c.Type == roleOrClaim && c.Value == "true");
    }
}