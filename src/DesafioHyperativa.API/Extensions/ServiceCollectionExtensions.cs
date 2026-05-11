using DesafioHyperativa.Application.Validators;
using DesafioHyperativa.Infrastructure.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

namespace DesafioHyperativa.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        // Serilog
        builder.Host.UseSerilog((ctx, lc) => lc
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/app-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30
            ));

        // Infrastructure (DB, Repos, Services)
        builder.Services.AddInfrastructure(configuration);

        // FluentValidation
        builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        // Controllers
        builder.Services.AddControllers();

        // JWT Authentication
        var jwtSection = configuration.GetSection("JwtSettings");
        var secretKey = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey não configurada.");

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        builder.Services.AddAuthorization();

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
                policy.WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"])
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        // Rate Limiting
        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("fixed", limiterOptions =>
            {
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.PermitLimit = 60;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 10;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        // Health Checks
        builder.Services.AddHealthChecks()
            .AddNpgSql(
                connectionString: configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
                name: "postgresql",
                tags: ["db", "ready"]);

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Desafio Hyperativa - Card API",
                Version = "v1",
                Description = "API REST segura para cadastro e consulta de números de cartões.",
                Contact = new OpenApiContact { Name = "Hyperativa", Email = "contato@hyperativa.com.br" }
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Informe o JWT Bearer token. Exemplo: Bearer {seu_token}"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            // Incluir comentários XML para documentação
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath);
        });

        return builder;
    }
}
