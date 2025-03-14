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

    var builder = WebApplication.CreateBuilder(args);

    // Configuration dosyalarýný ekleyin
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables();

    // JWT Authentication yapýlandýrmasý
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
            policy => policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    // Controller'larý ekleyin
    builder.Services.AddControllers();

    // Health checks ekleyin
    builder.Services.AddHealthChecks();

    // Serilog'u kullan
    builder.Host.UseSerilog();

    var app = builder.Build();

    // HTTP Request pipeline'ý yapýlandýrýn
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    //app.UseSerilog();
    app.UseCors("CorsPolicy");

    // Önce endpoint'ler tanýmlanmalý
    app.UseRouting();

    // Authentication ve Authorization middleware'lerini ekleyin
    app.UseAuthentication();
    app.UseAuthorization();

    // Özel endpoint'ler tanýmlanmalý - ÖNEMLÝ: UseOcelot'dan ÖNCE
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapControllers();

    // Ocelot middleware'ini en son kullanýn
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