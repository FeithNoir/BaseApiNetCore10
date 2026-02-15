using BaseApiNet10.Business.Auxiliar;
using BaseApiNet10.Business.DoI;
using BaseApiNet10.Core.Entities.Models;
using BaseApiNet10.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BaseApiNet10", Version = "v1" });
});

// Agrega el contexto de base de datos utilizando ContextFactory
builder.Services.AddDbContext(builder.Configuration);

// Agrega los servicios personalizados de repositorios y servicios
builder.Services.AddCustomServices();

// Agrega los servicios personalizados de autenticación y correo electrónico
builder.Services.AddCustomAuthenticationAndEmailServices(builder.Configuration);

builder.Services.AddIdentity<User, IdentityRole>(
options =>
        options.SignIn.RequireConfirmedAccount = false)
        .AddEntityFrameworkStores<Context>()
        .AddDefaultTokenProviders();

// Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "BaseApiNet10 V1");
    });
}

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
