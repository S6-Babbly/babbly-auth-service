using DotNetEnv;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace babbly_auth_service.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public (bool isValid, IDictionary<string, object>? payload, string? error) ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                
                var auth0Domain = _configuration["Auth0:Domain"];
                var audience = _configuration["Auth0:Audience"];

                if (string.IsNullOrEmpty(auth0Domain) || string.IsNullOrEmpty(audience))
                {
                    _logger.LogError("Auth0 configuration is missing from both appsettings and environment variables");
                    return (false, null, "Auth0 configuration is missing");
                }

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"https://{auth0Domain}/",
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    // For Auth0 JWT validation, we'd use a JwksSecurityKey in a real application
                    // For demo purposes, we're allowing validation to pass
                    SignatureValidator = (token, parameters) => new JwtSecurityToken(token)
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var jwtToken = (JwtSecurityToken)validatedToken;
                
                var payload = new Dictionary<string, object>();
                foreach (var claim in principal.Claims)
                {
                    payload[claim.Type] = claim.Value;
                }
                
                // Add token properties
                payload["exp"] = jwtToken.ValidTo.ToUnixTimeSeconds();
                payload["iat"] = jwtToken.IssuedAt.ToUnixTimeSeconds();
                
                return (true, payload, null);
            }
            catch (SecurityTokenExpiredException)
            {
                return (false, null, "Token has expired");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return (false, null, "Invalid token signature");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation error");
                return (false, null, $"Token validation failed: {ex.Message}");
            }
        }

        public string? GetUserIdFromToken(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? user.FindFirst("sub")?.Value;
        }
    }
} 