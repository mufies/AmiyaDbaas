using System.Text;
using AmiyaDbaasManager.Data;
using AmiyaDbaasManager.Middlewares;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories;
using AmiyaDbaasManager.Repositories.Interfaces;
using AmiyaDbaasManager.Services;
using AmiyaDbaasManager.Services.interfaces;
using AmiyaDbaasManager.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

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
    });

// ─── DI Repositories & Services ───────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDbInstanceRepo, DbInstanceRepo>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDockerService, DockerService>();
builder.Services.AddScoped<IPortManagerService, PortManagerService>();
builder.Services.AddScoped<IDbInstanceService, DbInstanceService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IUserSubscriptionService, UserSubscriptionService>();
builder.Services.AddHostedService<ContainerHealthCheckWorker>();

// ─── Controllers & Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers().AddJsonOptions(options =>
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
