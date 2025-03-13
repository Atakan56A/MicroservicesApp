using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Cache.CacheManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Serilog;

// Builder tan�m�ndan �nce:
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Gateway API");
    // builder tan�m�...

    var builder = WebApplication.CreateBuilder(args);

    // Ocelot servislerini ekleyin k�sm�ndan �nce ekleyin:
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

    // CORS politikas�n� ekleyin
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy",
            builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    // Controller'lar� ekleyin (Health check vb. i�in)
    builder.Services.AddControllers();

    // Controller'lar� ekleyin k�sm�ndan sonra ekleyin:
    builder.Services.AddHealthChecks();

    // builder.Host k�sm�na ekleyin:
    builder.Host.UseSerilog();

    var app = builder.Build();

    // HTTP Request pipeline'� yap�land�r�n
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseCors("CorsPolicy");
    app.UseRouting();

    // Ocelot middleware'ini kullan�n k�sm�ndan �nce ekleyin:
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Ocelot middleware'ini kullan�n
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
