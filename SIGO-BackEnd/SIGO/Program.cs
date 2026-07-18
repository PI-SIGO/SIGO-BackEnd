using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF;
using QuestPDF.Infrastructure;
using Refit;
using SIGO.Data;
using SIGO.Data.Interfaces;
using SIGO.Data.Repositories;
using SIGO.Data.Seed;
using SIGO.Errors;
using SIGO.Filters;
using SIGO.Integracao;
using SIGO.Integracao.Interfaces;
using SIGO.Integracao.Prefit;
using SIGO.Objects.Dtos.Mappings;
using SIGO.Objects.Models;
using SIGO.Middleware;
using SIGO.Security;
using SIGO.Services.Entities;
using SIGO.Services.Interfaces;
using SIGO.Validation;
using System;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Swashbuckle.AspNetCore.SwaggerGen;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

const string FrontendCorsPolicy = "FrontendCorsPolicy";
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(defaultConnection))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured through environment variables or user-secrets.");

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiErrorResponseFilter>();
    options.Filters.Add<FluentValidationActionFilter>();
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = ApiProblemDetailsFactory.CreateValidation(
            context.HttpContext,
            context.ModelState);
        var result = new UnprocessableEntityObjectResult(problemDetails)
        {
            DeclaredType = problemDetails.GetType()
        };
        result.ContentTypes.Add(ApiProblemDetailsFactory.ProblemContentType);

        return result;
    };
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddValidatorsFromAssemblyContaining<CadastrarClienteValidator>();
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        JwtOptions.IsValid,
        "Configure Jwt:Issuer, Jwt:Audience, and Jwt:Key with at least 32 bytes through environment variables or user-secrets.")
    .ValidateOnStart();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    var knownProxies = builder.Configuration
        .GetSection("ForwardedHeaders:KnownProxies")
        .Get<string[]>() ?? Array.Empty<string>();

    if (knownProxies.Length == 0)
        return;

    options.KnownProxies.Clear();

    foreach (var proxy in knownProxies)
    {
        if (!IPAddress.TryParse(proxy, out var ipAddress))
            throw new InvalidOperationException($"ForwardedHeaders:KnownProxies contains invalid IP address '{proxy}'.");

        options.KnownProxies.Add(ipAddress);
    }
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(opt => { }, AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SIGO API",
        Version = "v1",
        Description = "API versionada para gestão de oficinas, clientes, veículos e ordens de serviço."
    });

    options.TagActionsBy(apiDescription =>
    {
        var controllerName = apiDescription.ActionDescriptor.RouteValues["controller"] ?? "Outros";
        return controllerName.StartsWith("Cliente", StringComparison.Ordinal)
            ? new[] { "Cliente" }
            : new[] { controllerName };
    });

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Bearer token. Inform only the token value; Swagger UI sends the Authorization: Bearer header."
    });

    options.OperationFilter<AuthorizeOperationFilter>();
});
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
    opt.UseNpgsql(defaultConnection);
});
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddCheck<DbContextReadinessHealthCheck>("database", tags: new[] { "ready" });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        await ApiProblemDetailsFactory.WriteAsync(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            detail: "Muitas tentativas. Tente novamente mais tarde.",
            type: ApiProblemTypes.RateLimit,
            cancellationToken: token);
    };

    options.AddPolicy(RateLimitPolicies.ClienteLogin, httpContext =>
        CreateIpFixedWindowLimiter(httpContext, RateLimitPolicies.ClienteLogin, 5, TimeSpan.FromMinutes(1)));

    options.AddPolicy(RateLimitPolicies.OficinaLogin, httpContext =>
        CreateIpFixedWindowLimiter(httpContext, RateLimitPolicies.OficinaLogin, 5, TimeSpan.FromMinutes(1)));

    options.AddPolicy(RateLimitPolicies.FuncionarioLogin, httpContext =>
        CreateIpFixedWindowLimiter(httpContext, RateLimitPolicies.FuncionarioLogin, 5, TimeSpan.FromMinutes(1)));

    options.AddPolicy(RateLimitPolicies.PublicRegistration, httpContext =>
        CreateIpFixedWindowLimiter(httpContext, RateLimitPolicies.PublicRegistration, 3, TimeSpan.FromMinutes(5)));

    options.AddPolicy(RateLimitPolicies.ClientePreRegistration, httpContext =>
        CreateActorIpFixedWindowLimiter(httpContext, RateLimitPolicies.ClientePreRegistration, 20, TimeSpan.FromHours(1)));

    options.AddPolicy(RateLimitPolicies.ClientePasswordChange, httpContext =>
        CreateActorIpFixedWindowLimiter(httpContext, RateLimitPolicies.ClientePasswordChange, 5, TimeSpan.FromMinutes(15)));
});
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IClienteOficinaRepository, ClienteOficinaRepository>();
builder.Services.AddScoped<IClienteContaRepository, ClienteContaRepository>();
builder.Services.AddScoped<IClienteIdentityRepository, ClienteIdentityRepository>();
builder.Services.AddScoped<IClienteRegistrationService, ClienteRegistrationService>();
builder.Services.AddScoped<IClienteAuthenticationService, ClienteAuthenticationService>();
builder.Services.AddScoped<IClienteVinculoService, ClienteVinculoService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<ITelefoneRepository, TelefoneRepository>();
builder.Services.AddScoped<ITelefoneService, TelefoneService>();

builder.Services.AddScoped<IServicoRepository, ServicoRepository>();
builder.Services.AddScoped<IServicoService, ServicoService>();

builder.Services.AddScoped<IMarcaService, MarcaService>();
builder.Services.AddScoped<IMarcaRepository, MarcaRepository>();

builder.Services.AddScoped<IVeiculoService, VeiculoService>();
builder.Services.AddScoped<IVeiculoRepository, VeiculoRepository>();
builder.Services.AddScoped<IVeiculoImagemStorageService, LocalVeiculoImagemStorageService>();

builder.Services.AddScoped<IFuncionarioService, FuncionarioService>();
builder.Services.AddScoped<IFuncionarioRepository, FuncionarioRepository>();

builder.Services.AddScoped<IPecaService, PecaService>();
builder.Services.AddScoped<IPecaRepository, PecaRepository>();

builder.Services.AddScoped<IOficinaService, OficinaService>();
builder.Services.AddScoped<IOficinaRepository, OficinaRepository>();

builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IFuncionarioRoleResolver, FuncionarioRoleResolver>();
builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
builder.Services.AddScoped<ICpfValidator, CpfValidator>();
builder.Services.AddScoped<ICnpjValidator, CnpjValidator>();
builder.Services.AddScoped<ICpfCnpjValidator, CpfCnpjValidator>();
builder.Services.AddScoped<IViaCepIntegracao, ViaCepIntegracao>();
builder.Services.AddScoped<IRegistroServicoRepository, RegistroServicoRepository>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<DevelopmentDatabaseSeeder>();
builder.Services.AddRefitClient<IViaCepIntegracaoRefit>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("https://viacep.com.br/");
    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
    {
        var jwtOptions = jwtOptionsAccessor.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Key))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdValue, out var userId))
                {
                    context.Fail("Token is missing the required user identifier.");
                    return;
                }

                if (context.Principal.IsInRole(SystemRoles.Cliente))
                {
                    var tokenVersionValue = context.Principal
                        .FindFirst(CustomClaimTypes.TokenVersion)?.Value;
                    if (!int.TryParse(tokenVersionValue, out var tokenVersion))
                    {
                        context.Fail("Cliente token is missing the required version.");
                        return;
                    }

                    var contaRepository = context.HttpContext.RequestServices
                        .GetRequiredService<IClienteContaRepository>();
                    var currentVersion = await contaRepository.GetActiveTokenVersionAsync(
                        userId,
                        context.HttpContext.RequestAborted);

                    if (!currentVersion.HasValue || currentVersion.Value != tokenVersion)
                        context.Fail("Cliente token has been revoked.");

                    return;
                }

                if (context.Principal.IsInRole(SystemRoles.Oficina))
                {
                    var oficinaClaim = context.Principal
                        .FindFirst(CustomClaimTypes.OficinaId)?.Value;
                    if (!int.TryParse(oficinaClaim, out var oficinaId) || oficinaId != userId)
                    {
                        context.Fail("Oficina token has inconsistent tenant claims.");
                        return;
                    }

                    var oficinaRepository = context.HttpContext.RequestServices
                        .GetRequiredService<IOficinaRepository>();
                    if (!await oficinaRepository.IsActiveAsync(
                            userId,
                            context.HttpContext.RequestAborted))
                    {
                        context.Fail("Oficina token has been revoked.");
                    }

                    return;
                }

                if (context.Principal.IsInRole(SystemRoles.Funcionario) ||
                    context.Principal.IsInRole(SystemRoles.Admin))
                {
                    var funcionarioRepository = context.HttpContext.RequestServices
                        .GetRequiredService<IFuncionarioRepository>();
                    var funcionario = await funcionarioRepository.GetActiveByIdAsync(
                        userId,
                        context.HttpContext.RequestAborted);
                    if (funcionario is null)
                    {
                        context.Fail("Funcionario token has been revoked.");
                        return;
                    }

                    var currentRole = string.Equals(
                        funcionario.Role,
                        SystemRoles.Admin,
                        StringComparison.OrdinalIgnoreCase)
                        ? SystemRoles.Admin
                        : SystemRoles.Funcionario;
                    var tokenRole = context.Principal.FindFirst(ClaimTypes.Role)?.Value;
                    if (!string.Equals(tokenRole, currentRole, StringComparison.Ordinal))
                    {
                        context.Fail("Funcionario permissions changed after token issuance.");
                        return;
                    }

                    if (currentRole == SystemRoles.Funcionario)
                    {
                        var oficinaClaim = context.Principal
                            .FindFirst(CustomClaimTypes.OficinaId)?.Value;
                        if (!int.TryParse(oficinaClaim, out var oficinaId) ||
                            funcionario.IdOficina != oficinaId)
                        {
                            context.Fail("Funcionario tenant changed after token issuance.");
                        }
                    }
                }
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();

                await ApiProblemDetailsFactory.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    detail: "Token de autenticacao ausente, invalido ou expirado.",
                    type: ApiProblemTypes.Unauthorized,
                    cancellationToken: context.HttpContext.RequestAborted);
            },
            OnForbidden = context => ApiProblemDetailsFactory.WriteAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                detail: "O usuario autenticado nao possui permissao para acessar este recurso.",
                type: ApiProblemTypes.Forbidden,
                cancellationToken: context.HttpContext.RequestAborted)
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

var app = builder.Build();

if (args.Contains("--seed", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider
        .GetRequiredService<DevelopmentDatabaseSeeder>()
        .SeedAsync();

    return;
}

app.UseForwardedHeaders();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseStatusCodePages(statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    var statusCode = httpContext.Response.StatusCode;

    if (statusCode < StatusCodes.Status400BadRequest || statusCode >= 600)
        return Task.CompletedTask;

    return ApiProblemDetailsFactory.WriteAsync(
        httpContext,
        statusCode,
        cancellationToken: httpContext.RequestAborted);
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthCheckResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthCheckResponse
}).AllowAnonymous();

app.MapControllers();

app.Run();

static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    return context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
        checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => new
            {
                status = entry.Value.Status.ToString(),
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
            })
    });
}

static RateLimitPartition<string> CreateIpFixedWindowLimiter(
    HttpContext httpContext,
    string policyName,
    int permitLimit,
    TimeSpan window)
{
    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var partitionKey = $"{policyName}:{ipAddress}";

    return RateLimitPartition.GetFixedWindowLimiter(
        partitionKey,
        _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = permitLimit,
            QueueLimit = 0,
            Window = window
        });
}

static RateLimitPartition<string> CreateActorIpFixedWindowLimiter(
    HttpContext httpContext,
    string policyName,
    int permitLimit,
    TimeSpan window)
{
    var actorId = httpContext.User.FindFirst(CustomClaimTypes.OficinaId)?.Value
        ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? "anonymous";
    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var partitionKey = $"{policyName}:{actorId}:{ipAddress}";

    return RateLimitPartition.GetFixedWindowLimiter(
        partitionKey,
        _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = permitLimit,
            QueueLimit = 0,
            Window = window
        });
}

internal sealed class DbContextReadinessHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public DbContextReadinessHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _dbContext.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Healthy();

            return HealthCheckResult.Unhealthy("Database is unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database readiness check failed.", ex);
        }
    }
}

internal sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!RequiresAuthorization(context))
            return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            }] = Array.Empty<string>()
        });
    }

    private static bool RequiresAuthorization(OperationFilterContext context)
    {
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        if (endpointMetadata.OfType<IAllowAnonymous>().Any())
            return false;

        if (endpointMetadata.OfType<IAuthorizeData>().Any())
            return true;

        var methodInfo = context.MethodInfo;
        if (methodInfo.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any())
            return false;

        if (methodInfo.DeclaringType?.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any() == true)
            return false;

        return methodInfo.GetCustomAttributes(true).OfType<IAuthorizeData>().Any()
            || methodInfo.DeclaringType?.GetCustomAttributes(true).OfType<IAuthorizeData>().Any() == true;
    }
}
