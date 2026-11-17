using System.Text;
using System.Text.Json.Serialization;
using AssetFlow.Api.Data;
using AssetFlow.Api.Domain;
using AssetFlow.Api.Infrastructure;
using AssetFlow.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database (PostgreSQL) ─────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "No database connection string. Run: dotnet user-secrets set \"ConnectionStrings:Default\" " +
        "\"Host=localhost;Port=5432;Database=assetflow;Username=postgres;Password=YOUR_PASSWORD\"");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// ── Authentication (JWT) ─────────────────────────────────────────────────
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key must be set and at least 32 characters long.");
builder.Services.AddSingleton(jwt);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep the claim names exactly as we wrote them ("sub", "role")
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

// ── Application services (dependency injection) ──────────────────────────
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// Let the React frontend (next step) call this API from the browser
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ── API documentation (OpenAPI + Scalar UI at /scalar) ───────────────────
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "AssetFlow API";
    document.Info.Description = "IT asset and equipment loan management.";
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Log in with POST /api/auth/login and paste the token here.",
    };
    return Task.CompletedTask;
}));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference(o => o.WithTitle("AssetFlow API").AddPreferredSecuritySchemes("Bearer"));
app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
app.MapControllers();

await DatabaseSetup.InitialiseAsync(app.Services);
app.Run();

// Lets the test project start the app in memory
public partial class Program;
