using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shelter.Domain.Common;

namespace Shelter.Api.Configuration;

internal sealed class DomainExceptionHandler(IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            DomainValidationException    => (StatusCodes.Status400BadRequest, "Validation failed"),
            DomainAuthorizationException => (StatusCodes.Status403Forbidden,  "Forbidden"),
            DomainNotFoundException      => (StatusCodes.Status404NotFound,   "Not found"),
            _ => (0, null!),
        };

        if (status == 0) return false;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
        };

        if (environment.IsDevelopment())
        {
            problem.Extensions["exceptionType"] = exception.GetType().FullName;
            problem.Extensions["stackTrace"] = exception.ToString();
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
