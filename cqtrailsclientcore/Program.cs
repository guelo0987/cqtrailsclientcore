using System.Text;
using cqtrailsclientcore.Context;
using cqtrailsclientcore.Utils;
using DotEnv.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

// Enable legacy timestamp behavior for PostgreSQL to handle DateTime values properly
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Configuración de URL solo para entornos de despliegue como Railway
// En desarrollo, usará los valores de launchSettings.json
if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    Console.WriteLine($"Configurando para entorno de producción en puerto {port}");
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}
else
{
    Console.WriteLine("Usando configuración de puerto de desarrollo desde launchSettings.json");
}

//ENV LOAD
try 
{
    if (File.Exists("development.env"))
    {
        new EnvLoader().AddEnvFile("development.env").Load();
        Console.WriteLine("Environment file development.env loaded successfully");
    }
    else
    {
        Console.WriteLine("Environment file development.env not found, using environment variables from Railway");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Failed to load environment file: {ex.Message}");
    Console.WriteLine("Continuing with Railway environment variables");
}

//DBConnection
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("DATA_BASE_CONNECTION_STRING")));

// Registrar el servicio de Google Drive
builder.Services.AddScoped<cqtrailsclientcore.Utils.GoogleDriveService>(provider => {
    var webHostEnvironment = provider.GetRequiredService<IWebHostEnvironment>();
    var logger = provider.GetRequiredService<ILogger<cqtrailsclientcore.Utils.GoogleDriveService>>();
    return new cqtrailsclientcore.Utils.GoogleDriveService(webHostEnvironment.WebRootPath, logger);
});

// Registrar el servicio de Email
builder.Services.AddScoped<EmailService>();

// Add health checks
builder.Services.AddHealthChecks();


// ADD CONTROLLERS
builder.Services.AddControllers(option => option.ReturnHttpNotAcceptable = true)
    .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
    .AddXmlDataContractSerializerFormatters();


//JWT CONFIGURATION
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;  // Disable HTTPS requirement for JWT authentication
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")))
        }; 
    });

//Configuracion de politicas de autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireClienteRole", policy => policy.RequireRole("Cliente"));
});


builder.Services.AddEndpointsApiExplorer();
//SWAGUER API CONFIGURATION

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CQTRAILS API", Version = "v1" });

    // Configuración para JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


//cors configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder => builder
            .WithOrigins("http://localhost:3000", "https://cqtrailsclientcore-production.up.railway.app") 
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

// Configure Swagger for both development and production
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CQTRAILS API v1");
    c.ConfigObject.DisplayRequestDuration = true;
    c.RoutePrefix = "swagger";
});

app.UseRouting();
app.UseCors("AllowReactApp");

// HTTPS redirection disabled for Railway deployment
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Añadir middleware para servir archivos estáticos desde wwwroot
app.UseStaticFiles();

// Map health check endpoint for Railway
app.MapHealthChecks("/health");

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

