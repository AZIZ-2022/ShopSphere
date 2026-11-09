using ECommerce.Web.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[Route("error")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger) => _logger = logger;

    [Route("{code:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Status(int code)
    {
        // Preserve the real status code for the response.
        Response.StatusCode = code;

        var exFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exFeature is not null)
        {
            _logger.LogError(exFeature.Error, "Unhandled exception on {Path}", exFeature.Path);
        }

        var vm = new ErrorPageViewModel
        {
            StatusCode = code,
            Title = code switch
            {
                404 => "Page not found",
                403 => "Access denied",
                400 => "Bad request",
                >= 500 => "Something went wrong",
                _ => "Unexpected error"
            },
            Message = code switch
            {
                404 => "The page you're looking for doesn't exist or has moved.",
                403 => "You don't have permission to view this page.",
                >= 500 => "An unexpected error occurred on our side. Please try again in a moment.",
                _ => "Your request could not be completed."
            },
            RequestId = HttpContext.TraceIdentifier
        };

        return View("Status", vm);
    }
}
