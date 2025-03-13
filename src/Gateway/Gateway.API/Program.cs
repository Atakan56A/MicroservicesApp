using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Cache.CacheManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Serilog;

// Builder tanýmýndan önce:
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Gateway API");
    // builder tanýmý...

    var builder = WebApplication.CreateBuilder(args);

    // Ocelot servislerini ekleyin kýsmýndan önce ekleyin:
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            };
        });

    // Ocelot servislerini ekleyin
    builder.Services.AddOcelot(builder.Configuration)
        .AddCacheManager(x => {
            x.WithDictionaryHandle();
        });

    // CORS politikasýný ekleyin
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy",
            builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    // Controller'larý ekleyin (Health check vb. için)
    builder.Services.AddControllers();

    // Controller'larý ekleyin kýsmýndan sonra ekleyin:
    builder.Services.AddHealthChecks();

    // builder.Host kýsmýna ekleyin:
    builder.Host.UseSerilog();

    var app = builder.Build();

    // HTTP Request pipeline'ý yapýlandýrýn
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseCors("CorsPolicy");
    app.UseRouting();

    // Ocelot middleware'ini kullanýn kýsmýndan önce ekleyin:
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Ocelot middleware'ini kullanýn
    await app.UseOcelot();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Gateway API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
