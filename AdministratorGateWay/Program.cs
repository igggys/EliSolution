using System.Text;
using AdministratorGateWay.Infrastructure;
using Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers();
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 20 * 1024 * 1024;
    });
    builder.Services.AddProblemDetails();
    builder.Services.Configure<SwaggerSettings>(
        builder.Configuration.GetSection(SwaggerSettings.SectionName));
    var swaggerSettings = builder.Configuration.GetSection(SwaggerSettings.SectionName).Get<SwaggerSettings>()
        ?? new SwaggerSettings();
    if (swaggerSettings.Enabled)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "EliSolution Admin API",
                Version = "v1",
                Description = "Server API for the public site administration panel."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT from POST /api/auth/login"
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });
    }

    builder.Services.Configure<DatabaseConnection>(
        builder.Configuration.GetSection(DatabaseConnection.SectionName));
    builder.Services.Configure<AdministratorDatabaseConnection>(
        builder.Configuration.GetSection(AdministratorDatabaseConnection.SectionName));
    builder.Services.AddSingleton<BlogImageStorage>();

    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection(JwtSettings.SectionName));
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
        ?? throw new InvalidOperationException("Jwt configuration section is missing.");
    if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey) || jwtSettings.SigningKey.Length < 32)
    {
        throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
    }

    builder.Services.AddSingleton<DataManager.AdministratorSite.DataManager>(serviceProvider =>
    {
        var databaseConnection = serviceProvider.GetRequiredService<IOptions<DatabaseConnection>>().Value;
        return new DataManager.AdministratorSite.DataManager(databaseConnection.ConnectionString);
    });
    builder.Services.AddSingleton<DataManager.AdministratorSite.AuthDataManager>(serviceProvider =>
    {
        var databaseConnection = serviceProvider.GetRequiredService<IOptions<AdministratorDatabaseConnection>>().Value;
        return new DataManager.AdministratorSite.AuthDataManager(databaseConnection.ConnectionString);
    });
    builder.Services.AddSingleton<TokenService>();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (allowedOrigins.Length > 0)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });
    }

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    if (swaggerSettings.Enabled)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    if (allowedOrigins.Length > 0)
    {
        app.UseCors();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AdministratorGateWay terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
