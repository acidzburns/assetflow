using AssetFlow.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AssetFlow.Api.Infrastructure;

/// <summary>
/// Turns business errors into clean HTTP responses (RFC 7807 "problem details"):
///   NotFoundException  → 404
///   ConflictException  → 409
///   DomainException    → 400
/// Anything else is a real bug → 500 (details are only logged, never shown).
/// </summary>
public class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainException => (StatusCodes.Status400BadRequest, "Request not allowed"),
            _ => (0, ""),
        };
        if (status == 0) return false;

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = exception.Message },
        });
    }
}
