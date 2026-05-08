using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NZWalks.API.Data;
using NZWalks.API.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger with description ──────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NZWalks + Inventory API",
        Version = "v1",
        Description = "Inventory Management System API. " +
                      "Before testing Products, create a Category first using " +
                      "POST /api/inventory/categories (not yet implemented — " +
                      "insert a row directly in SQL for now, or seed one below)."
    });
});

// ── DbContexts ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<NZWalksDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NZWalkerConnectionString")));

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NZWalkerConnectionString")));

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IProductRepository, ProductRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1");
        c.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();