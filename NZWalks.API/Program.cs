using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<NZWalksDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NZWalkerConnectionString")));

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NZWalkerConnectionString")));

// ADD THIS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNuxtApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ADD THIS (must be before UseAuthorization)
app.UseCors("AllowNuxtApp");

app.UseAuthorization();

app.MapControllers();

app.Run();