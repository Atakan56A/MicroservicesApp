using Microsoft.EntityFrameworkCore;
using Tenant.API.Data;
using Tenant.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add database connection
builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services
builder.Services.AddScoped<ITenantService, TenantService>();

// Add controllers
builder.Services.AddControllers();

// Add OpenAPI
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.MapControllers();

app.Run();