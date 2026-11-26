using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BaseApi.Api.Infrastructure;

/// <summary>
/// Antes de cada acción busca un IValidator&lt;T&gt; para cada argumento y, si falla,
/// responde 400 con Problem Details y el mapa "errors" (campo -> mensajes).
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        foreach (var arg in ctx.ActionArguments.Values)
        {
            if (arg is null) continue;
            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(arg), ctx.HttpContext.RequestAborted);
            if (result.IsValid) continue;

            var errors = result.Errors
                .GroupBy(e => char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..]) // camelCase
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            ctx.Result = new ObjectResult(new ValidationProblemDetails(errors)
            {
                Type = "about:blank",
                Title = "Datos inválidos",
                Status = 400,
                Detail = "Revisa los campos marcados",
            })
            { StatusCode = 400, ContentTypes = { "application/problem+json" } };
            return;
        }
        await next();
    }
}
