using BaseApi.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BaseApi.Api.Infrastructure;

/// <summary>IExceptionHandler (.NET 8+): convierte excepciones en Problem Details (RFC 9457).</summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problems, ILogger<DomainExceptionHandler> log)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title, detail) = ex switch
        {
            DomainException d => d.Kind switch
            {
                DomainErrorKind.Validation => (400, "Datos inválidos", d.Message),
                DomainErrorKind.InvalidToken => (400, "Token inválido", d.Message),
                DomainErrorKind.Conflict => (409, "Conflicto", d.Message),
                DomainErrorKind.Unauthorized => (401, "No autorizado", d.Message),
                DomainErrorKind.Forbidden => (403, "Correo sin verificar", d.Message),
                _ => (500, "Error interno", "Ocurrió un error inesperado"),
            },
            BadHttpRequestException => (400, "Datos inválidos", "El cuerpo de la petición no es JSON válido"),
            _ => (500, "Error interno", "Ocurrió un error inesperado"),
        };
        if (status == 500) log.LogError(ex, "Error no controlado");

        http.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = new ProblemDetails { Type = "about:blank", Title = title, Status = status, Detail = detail },
        });
    }
}
