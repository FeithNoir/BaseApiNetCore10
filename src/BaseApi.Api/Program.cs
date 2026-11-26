using System.Text;
using BaseApi.Api.Infrastructure;
using BaseApi.Application;
using BaseApi.Infrastructure;
using BaseApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
builder.WebHost.UseUrls($"http://0.0.0.0:{config["PORT"] ?? "8080"}");

// ---- Capas (composition root) ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(config);

// ---- API ----
builder.Services.AddControllers(o => o.Filters.Add<ValidationFilter>());
// JSON mal formado o tipos equivocados: [ApiController] responde 400 antes de llegar a la acción.
builder.Services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ctx =>
    new BadRequestObjectResult(new ProblemDetails
    {
        Type = "about:blank", Title = "Datos inválidos", Status = 400, Detail = "El cuerpo de la petición no es válido",
    })
    { ContentTypes = { "application/problem+json" } });
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = c => c.ProblemDetails.Extensions.Remove("traceId"));
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// ---- JWT: solo HS256, y se validan firma, exp, iss y aud ----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = config["JWT_ISSUER"] ?? "baseapi",
        ValidAudience = config["JWT_AUDIENCE"] ?? "baseapi-clients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT_SECRET"] ?? "")),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.FromSeconds(30),
    };
    o.Events = new JwtBearerEvents
    {
        OnChallenge = async ctx =>
        {
            ctx.HandleResponse(); // en vez del 401 vacío, Problem Details
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "about:blank", Title = "No autorizado", Status = 401, Detail = "Falta el token o no es válido",
            }, options: null, contentType: "application/problem+json");
        },
    };
});
builder.Services.AddAuthorization();

// ---- OpenAPI con Swashbuckle (como en BaseApiNetCore10) ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Base API", Version = "v1" });
    o.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
    });
    o.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", doc)] = [],
    });
});

var app = builder.Build();

// Migraciones al arrancar: cómodo en desarrollo. En producción, mejor un paso aparte del despliegue.
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

app.UseExceptionHandler();
app.UseSwagger(o => o.RouteTemplate = "openapi/{documentName}.json");
app.UseSwaggerUI(o =>
{
    o.RoutePrefix = "docs";
    o.SwaggerEndpoint("/openapi/v1.json", "Base API v1");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();

public partial class Program; // para WebApplicationFactory en pruebas de integración
