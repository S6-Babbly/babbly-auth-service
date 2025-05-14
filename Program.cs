using babbly_auth_service.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using System.Security.Claims;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<KafkaProducerService>();

builder.Services.AddControllers();

// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Get Auth0 configuration
var auth0Domain = Environment.GetEnvironmentVariable("AUTH0_DOMAIN") ?? 
                  builder.Configuration["Auth0:Domain"];
                  
var auth0Audience = Environment.GetEnvironmentVariable("AUTH0_AUDIENCE") ?? 
                    builder.Configuration["Auth0:Audience"];

if (string.IsNullOrEmpty(auth0Domain) || string.IsNullOrEmpty(auth0Audience))
{
    throw new InvalidOperationException("Auth0 Domain and Audience must be configured");
}

// Configure Auth0 authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = $"https://{auth0Domain}/";
    options.Audience = auth0Audience;
    
    // Configure token validation parameters
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Standard claims
        NameClaimType = "name",
        RoleClaimType = "https://babbly.com/roles",
        
        // Validation
        ValidateIssuer = true,
        ValidIssuer = $"https://{auth0Domain}/",
        ValidateAudience = true,
        ValidAudience = auth0Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        
        // Clock skew - allow a 5-minute difference between server and token
        ClockSkew = TimeSpan.FromMinutes(5)
    };
    
    // Configure metadata address
    var metadataAddress = $"https://{auth0Domain}/.well-known/openid-configuration";
    options.MetadataAddress = metadataAddress;
    
    // Event handlers for debugging
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context => 
        {
            // Successfully validated token
            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
            logger?.LogInformation("Token validated for subject: {Subject}", 
                context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                context.Principal?.FindFirstValue("sub"));
            return Task.CompletedTask;
        },
        
        OnAuthenticationFailed = context =>
        {
            // Failed token validation
            var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
            logger?.LogWarning("Token validation failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    // Base policy requiring authentication
    options.AddPolicy("authenticated", policy =>
        policy.RequireAuthenticatedUser());
        
    // Admin policy requiring the admin role
    options.AddPolicy("admin", policy =>
        policy.RequireRole("admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add CORS for development
app.UseCors(options => options
    .WithOrigins(
        "http://localhost:3000", // Frontend
        "http://localhost:5010"  // API Gateway
    )
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
);

app.UseHttpsRedirection();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
