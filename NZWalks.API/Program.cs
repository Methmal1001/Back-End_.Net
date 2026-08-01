using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NZWalks.API.Configuration;
using NZWalks.API.Data;
using NZWalks.API.Filters;
using NZWalks.API.Repositories;
using NZWalks.API.Repositories.HR;
using NZWalks.API.Repositories.WhatsApp;
using NZWalks.API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ActivityLogFilter>();
});
builder.Services.AddEndpointsApiExplorer();

// ── Swagger with JWT support ──────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NZWalks + Inventory + HR API",
        Version = "v1",
        Description = "Inventory Management System API. " +
                      "Before testing Products, create a Category first using " +
                      "POST /api/inventory/categories (not yet implemented — " +
                      "insert a row directly in SQL for now, or seed one below). " +
                      "HR endpoints require a Bearer token — login via POST /api/hr/auth/login first."
    });

    // Add JWT Bearer button to Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token here. Example: Bearer eyJhbGci..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── DbContexts ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<NZWalksDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NZWalkerConnectionString")));

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NZWalkerConnectionString")));

builder.Services.AddDbContext<HrDbContext>(options =>       // ← NEW: HR DbContext
    options.UseSqlServer(builder.Configuration.GetConnectionString("NZWalkerConnectionString")));

builder.Services.AddDbContext<RestaurantDbContext>(options =>  // ← NEW: Restaurant Management DbContext
    options.UseSqlServer(builder.Configuration.GetConnectionString("NZWalkerConnectionString")));

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero   // no grace period on expiry
    };
});

builder.Services.AddAuthorization();

// ── WhatsApp settings — NEW ─────────────────────────────────────────────────────
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsApp"));

// ── Repositories ──────────────────────────────────────────────────────────────
// Existing
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

// Restaurant Management — NEW
builder.Services.AddScoped<NZWalks.API.Repositories.Restaurant.IMenuRepository, NZWalks.API.Repositories.Restaurant.MenuRepository>();
builder.Services.AddScoped<NZWalks.API.Repositories.Restaurant.ITableRepository, NZWalks.API.Repositories.Restaurant.TableRepository>();
builder.Services.AddScoped<NZWalks.API.Repositories.Restaurant.IOrderRepository, NZWalks.API.Repositories.Restaurant.OrderRepository>();
builder.Services.AddScoped<NZWalks.API.Repositories.Restaurant.IBillingRepository, NZWalks.API.Repositories.Restaurant.BillingRepository>();

// HR — NEW
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IOvertimeRepository, OvertimeRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<IPerformanceRepository, PerformanceRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

// Activity Log — NEW
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();

// WhatsApp — NEW
builder.Services.AddScoped<IWhatsAppConversationRepository, WhatsAppConversationRepository>();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITokenService, TokenService>();

// Chatbot (Gemini-powered) — NEW
builder.Services.AddHttpClient<NZWalks.API.Services.ChatbotService>();
builder.Services.AddScoped<NZWalks.API.Services.IChatbotService, NZWalks.API.Services.ChatbotService>();

// WhatsApp (Cloud API adapter in front of the existing chatbot) — NEW
builder.Services.AddHttpClient<NZWalks.API.Services.WhatsAppMessagingService>();
builder.Services.AddScoped<NZWalks.API.Services.IWhatsAppMessagingService, NZWalks.API.Services.WhatsAppMessagingService>();
builder.Services.AddScoped<NZWalks.API.Services.IWhatsAppConversationService, NZWalks.API.Services.WhatsAppConversationService>();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

await NZWalks.API.Data.HrSeedData.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory + HR API v1");
        c.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();   // ← NEW: must come BEFORE UseAuthorization
app.UseAuthorization();

app.MapControllers();
app.Run();