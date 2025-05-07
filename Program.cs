using babbly_auth_service.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddHostedService<KafkaConsumerService>();

builder.Services.AddControllers();

// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configure Auth0 authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    // Get Auth0 settings from environment variables if empty in appsettings
    var auth0Domain = !string.IsNullOrEmpty(builder.Configuration["Auth0:Domain"]) 
        ? builder.Configuration["Auth0:Domain"] 
        : Env.GetString("AUTH0_DOMAIN");
        
    var auth0Audience = !string.IsNullOrEmpty(builder.Configuration["Auth0:Audience"]) 
        ? builder.Configuration["Auth0:Audience"] 
        : Env.GetString("AUTH0_AUDIENCE");
    
    options.Authority = $"https://{auth0Domain}/";
    options.Audience = auth0Audience;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name",
        RoleClaimType = "https://babbly.com/roles"
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
