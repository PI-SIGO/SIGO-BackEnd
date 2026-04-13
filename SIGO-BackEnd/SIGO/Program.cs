using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Refit;
using SIGO.Middleware;
using SIGO.Security;
using SIGO.Data;
using SIGO.Data.Interfaces;
using SIGO.Data.Repositories;
using SIGO.Integracao;
using SIGO.Integracao.Interfaces;
using SIGO.Integracao.Prefit;
using SIGO.Objects.Dtos.Mappings;
using SIGO.Objects.Models;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "FrontendCorsPolicy";

// Add services to the container.
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(opt => { }, AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;

                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1");
            })
            .AllowCredentials();
    });
});
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();

builder.Services.AddScoped<ITelefoneRepository, TelefoneRepository>();
builder.Services.AddScoped<ITelefoneService, TelefoneService>();

builder.Services.AddScoped<IServicoRepository, ServicoRepository>();
builder.Services.AddScoped<IServicoService, ServicoService>();

builder.Services.AddScoped<IMarcaService, MarcaService>();
builder.Services.AddScoped<IMarcaRepository, MarcaRepository>();

builder.Services.AddScoped<IVeiculoService, VeiculoService>();
builder.Services.AddScoped<IVeiculoRepository, VeiculoRepository>();

builder.Services.AddScoped<IFuncionarioService, FuncionarioService>();
builder.Services.AddScoped<IFuncionarioRepository, FuncionarioRepository>();

builder.Services.AddScoped<IPecaService, PecaService>();
builder.Services.AddScoped<IPecaRepository, PecaRepository>();

builder.Services.AddScoped<IOficinaService, OficinaService>();
builder.Services.AddScoped<IOficinaRepository, OficinaRepository>();

builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IViaCepIntegracao, ViaCepIntegracao>();
builder.Services.AddSingleton<IRequestLogStore, InMemoryRequestLogStore>();
builder.Services.AddRefitClient<IViaCepIntegracaoRefit>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("https://viacep.com.br/");
    });
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.ResponseStatusCode |
        HttpLoggingFields.Duration;
    options.CombineLogs = true;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.FullAccess, policy =>
        policy.RequireRole(SystemRoles.Admin, SystemRoles.Oficina));

    options.AddPolicy(AuthorizationPolicies.OperationalAccess, policy =>
        policy.RequireRole(SystemRoles.Admin, SystemRoles.Funcionario, SystemRoles.Oficina));

    options.AddPolicy(AuthorizationPolicies.SelfServiceAccess, policy =>
        policy.RequireRole(SystemRoles.Admin, SystemRoles.Funcionario, SystemRoles.Oficina, SystemRoles.Cliente));
});

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");
        var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;

        if (exception is not null)
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Erro interno no servidor."
        });
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsPolicy);
app.UseHttpLogging();
app.UseMiddleware<RequestAuditLoggingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
