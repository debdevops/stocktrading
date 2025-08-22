using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Shared.Common.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Stock Trading API Gateway", 
        Version = "v1",
        Description = "API Gateway for Stock Trading Microservices"
    });
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

// Configure JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? "")),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings?.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings?.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Ocelot configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot();

// Add HTTP clients with Polly for resilience
builder.Services.AddHttpClient("UserManagement", client =>
{
    client.BaseAddress = new Uri("http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("MarketData", client =>
{
    client.BaseAddress = new Uri("http://localhost:5002");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("TradingEngine", client =>
{
    client.BaseAddress = new Uri("http://localhost:5003");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("Portfolio", client =>
{
    client.BaseAddress = new Uri("http://localhost:5004");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("AIServices", client =>
{
    client.BaseAddress = new Uri("http://localhost:8000");
    client.Timeout = TimeSpan.FromSeconds(60);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stock Trading API Gateway V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

// Service status endpoint
app.MapGet("/api/gateway/status", async (IHttpClientFactory httpClientFactory) =>
{
    var services = new Dictionary<string, object>();
    var clients = new[] { "UserManagement", "MarketData", "TradingEngine", "Portfolio", "AIServices" };
    var ports = new[] { 5001, 5002, 5003, 5004, 8000 };

    for (int i = 0; i < clients.Length; i++)
    {
        try
        {
            var client = httpClientFactory.CreateClient(clients[i]);
            var response = await client.GetAsync("/health");
            services[clients[i]] = new
            {
                Status = response.IsSuccessStatusCode ? "Healthy" : "Unhealthy",
                Port = ports[i],
                ResponseTime = response.Headers.Date?.ToString() ?? "Unknown"
            };
        }
        catch (Exception ex)
        {
            services[clients[i]] = new
            {
                Status = "Unavailable",
                Port = ports[i],
                Error = ex.Message
            };
        }
    }

    return Results.Ok(new
    {
        Gateway = "Healthy",
        Services = services,
        Timestamp = DateTime.UtcNow
    });
});

app.MapControllers();

// Use Ocelot middleware
await app.UseOcelot();

app.Run();
