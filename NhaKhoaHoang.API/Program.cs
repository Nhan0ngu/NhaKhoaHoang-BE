
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NhaKhoaHoang.API.Controllers;
using NhaKhoaHoang.API.Data;
using NhaKhoaHoang.API.Hubs;
using NhaKhoaHoang.API.Middleware;
using NhaKhoaHoang.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ==================================================
// 1. DATABASE - POSTGRESQL (NEON)
// ==================================================

var connectionString = builder.Configuration
    .GetConnectionString("NhaKhoaDb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'NhaKhoaDb' is missing.");
}

builder.Services.AddDbContext<NhaKhoaDbContext>(options =>
    options.UseNpgsql(connectionString));

// ==================================================
// 2. CORS - FRONTEND
// ==================================================

const string frontendCorsPolicy = "FrontendPolicy";

var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

var allowedOrigins = configuredOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Where(origin =>
        Uri.TryCreate(
            origin,
            UriKind.Absolute,
            out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps ||
         (builder.Environment.IsDevelopment() &&
          uri.Scheme == Uri.UriSchemeHttp)))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // SignalR negotiation cross-origin.
        }
    });
});

// ==================================================
// 3. JWT AUTHENTICATION
// ==================================================

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32 ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT configuration is missing or invalid.");
}

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,
                RequireExpirationTime = true,

                ClockSkew = TimeSpan.Zero,

                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };

        // JWT hợp lệ nhưng Admin bị khóa trong DB
        // thì không được phép tiếp tục sử dụng API.

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var rawAdminId = context.Principal?
                    .FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (!int.TryParse(
                    rawAdminId,
                    out var adminId))
                {
                    context.Fail(
                        "Invalid admin identity.");
                    return;
                }

                var db = context.HttpContext
                    .RequestServices
                    .GetRequiredService<
                        NhaKhoaDbContext>();

                var isActiveAdmin = await db
                    .AdminUsers
                    .AsNoTracking()
                    .AnyAsync(
                        a => a.AdminId == adminId &&
                             a.IsActive,
                        context.HttpContext
                            .RequestAborted);

                if (!isActiveAdmin)
                {
                    context.Fail(
                        "Admin account is inactive.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// SignalR chi phat thong bao noi dung CONG KHAI.
builder.Services.AddSignalR();
builder.Services.AddScoped<PublicContentChangeFilter>();

// ==================================================
// 4. APPLICATION SERVICES
// ==================================================

builder.Services.AddScoped<AdminAuthService>();

// Chatbot OpenAI: key chỉ đọc từ cấu hình Backend (Render Environment).
builder.Services.AddHttpClient<OpenAiChatService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Azure tạm thời chưa sử dụng.
// Không đăng ký MediaStorageService
// khi chưa có Azure Connection String.

// ==================================================
// 5. CONTROLLERS
// ==================================================

builder.Services
    .AddControllers(options =>
        options.Filters.AddService<PublicContentChangeFilter>())
    .AddMvcOptions(options =>
    {
        // Tạm thời vô hiệu hóa Media API
        // do chưa cấu hình Azure Blob Storage.

        options.Conventions.Add(
            new DisableMediaControllerConvention());
    });

// ==================================================
// 6. RATE LIMITING
// ==================================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;

        context.HttpContext.Response.ContentType =
            "application/json; charset=utf-8";

        await context.HttpContext.Response
            .WriteAsJsonAsync(
                new
                {
                    success = false,
                    message =
                        "Bạn đã gửi quá nhiều yêu cầu. " +
                        "Vui lòng thử lại sau."
                },
                cancellationToken: ct);
    };

    // Consultation:
    // 5 requests / 10 minutes / IP

    options.AddPolicy(
        "consultations",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                    GetClientPartitionKey(
                        context,
                        "consultations"),

                factory: _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,

                        Window =
                            TimeSpan.FromMinutes(10),

                        QueueLimit = 0,

                        AutoReplenishment = true
                    }));

    // Chatbot: 12 lượt / 10 phút / IP, giới hạn sử dụng OpenAI.
    options.AddPolicy(
        "chat",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetClientPartitionKey(context, "chat"),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 12,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

    // Admin Login:
    // 5 requests / 15 minutes / IP

    options.AddPolicy(
        "admin-login",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                    GetClientPartitionKey(
                        context,
                        "admin-login"),

                factory: _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,

                        Window =
                            TimeSpan.FromMinutes(15),

                        QueueLimit = 0,

                        AutoReplenishment = true
                    }));
});

// ==================================================
// 7. SWAGGER
// ==================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "NhaKhoaHoang.API",
            Version = "v1",

            Description =
                "Backend API for Nha Khoa Hoang"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Enter your JWT access token."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,

                            Id = "Bearer"
                        }
                },

                Array.Empty<string>()
            }
        });
});

// ==================================================
// 8. BUILD APPLICATION
// ==================================================

var app = builder.Build();

// ==================================================
// 9. GLOBAL EXCEPTION HANDLING
// ==================================================

app.UseMiddleware<ApiExceptionMiddleware>();

// ==================================================
// 10. SWAGGER - DEVELOPMENT
// ==================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ==================================================
// 11. HTTP MIDDLEWARE PIPELINE
// ==================================================

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(frontendCorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Hub chỉ có thể nhận thông báo; client không được gọi phương thức CRUD.
app.MapHub<PublicContentHub>("/hubs/public-content")
    .RequireCors(frontendCorsPolicy);

// ==================================================
// 12. RUN APPLICATION
// ==================================================

app.Run();

// ==================================================
// HELPER - RATE LIMIT PARTITION
// ==================================================

static string GetClientPartitionKey(
    HttpContext context,
    string policyName)
{
    var ip = context.Connection.RemoteIpAddress?
        .MapToIPv6()
        .ToString() ?? "unknown";

    return $"{policyName}:{ip}";
}

// ==================================================
// TEMPORARY - DISABLE AZURE MEDIA API
// ==================================================

sealed class DisableMediaControllerConvention
    : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (controller.ControllerType.AsType() ==
            typeof(AdminMediaController))
        {
            controller.Actions.Clear();
        }
    }
}