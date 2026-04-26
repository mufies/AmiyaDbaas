using System.Text;
using System.Text.Json.Serialization;
using AmiyaDbaasManager.Data;
using AmiyaDbaasManager.Hubs;
using AmiyaDbaasManager.Middlewares;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories;
using AmiyaDbaasManager.Repositories.Interfaces;
using AmiyaDbaasManager.Services;
using AmiyaDbaasManager.Services.interfaces;
using AmiyaDbaasManager.Services.Interfaces;
using AmiyaDbaasManager.Services.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ─── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key chưa được cấu hình trong appsettings.");

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // Nếu request gửi tới hub của SignalR thì lấy token từ query string
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });

// ─── DI Repositories & Services ───────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDbInstanceRepo, DbInstanceRepo>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IDockerService, DockerService>();
builder.Services.AddScoped<IPortManagerService, PortManagerService>();
builder.Services.AddScoped<IDbInstanceService, DbInstanceService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IUserSubscriptionService, UserSubscriptionService>();
builder.Services.AddHostedService<ContainerHealthCheckWorker>();
builder.Services.AddHostedService<UserSubscriptionCheckWorker>();
builder.Services.AddSignalR();

// ─── Controllers & Swagger ────────────────────────────────────────────────────
builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AmiyaDbaasManager API", Version = "v1" });

    // Thêm JWT Auth vào Swagger UI
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description = "Nhập JWT token vào ô bên dưới (Swagger sẽ tự động thêm chữ Bearer)",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        }
    );
    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});

var app = builder.Build();

// ─── Middleware Pipeline ───────────────────────────────────────────────────────
// GlobalExceptionHandler phải đứng đầu để catch mọi exception
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.EnablePersistAuthorization();
    });
}
app.MapHub<InstanceLogs>("/hubs/instancelogs");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ─── Auto-apply migrations on startup ─────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // ─── Seed Plans (chỉ import khi bảng rỗng) ───────────────
    if (!db.Plans.Any())
    {
        db.Plans.AddRange(
            new Plan
            {
                Name = "Free",
                Description = "Gói miễn phí, phù hợp để thử nghiệm.",
                MaxInstances = 2,
                MaxCpuCoresPerInstance = 1,
                MaxRamMbPerInstance = 512,
                MaxStorageGbPerInstance = 5,
                PriceMonthly = 0m,
            },
            new Plan
            {
                Name = "Pro",
                Description = "Gói chuyên nghiệp cho cá nhân và nhóm nhỏ.",
                MaxInstances = 10,
                MaxCpuCoresPerInstance = 4,
                MaxRamMbPerInstance = 4096,
                MaxStorageGbPerInstance = 50,
                PriceMonthly = 100m,
            },
            new Plan
            {
                Name = "Enterprise",
                Description = "Gói doanh nghiệp không giới hạn tài nguyên.",
                MaxInstances = 50,
                MaxCpuCoresPerInstance = 8,
                MaxRamMbPerInstance = 16384,
                MaxStorageGbPerInstance = 500,
                PriceMonthly = 1000m,
            }
        );
        db.SaveChanges();
    }
}

app.Run();
