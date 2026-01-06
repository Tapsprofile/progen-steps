using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Procurement.Orchestration.Core.Exceptions;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> log)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unhandled exception");

            var (status, title) = ex switch
            {
                NotFoundException => (HttpStatusCode.NotFound, "Not found"),
                ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
                DomainException => (HttpStatusCode.Conflict, "Domain rule violation"),
                _ => (HttpStatusCode.InternalServerError, "Unhandled error")
            };

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = (int)status,
                Title = title,
                Detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}

