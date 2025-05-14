using DotNetEnv;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace babbly_auth_service.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        
        // In-memory authorization policy cache
        private readonly ConcurrentDictionary<string, bool> _authorizationCache;
        
        // JWKS configuration manager for token validation
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
        
        // Auth0 configuration
        private readonly string _domain;
        private readonly string _audience;
        private readonly string _issuer;

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _authorizationCache = new ConcurrentDictionary<string, bool>();
            
            // Set up Auth0 configuration
            _domain = Environment.GetEnvironmentVariable("AUTH0_DOMAIN") ?? 
                     configuration["Auth0:Domain"] ?? 
                     throw new InvalidOperationException("Auth0 Domain is not configured");
                     
            _audience = Environment.GetEnvironmentVariable("AUTH0_AUDIENCE") ?? 
                       configuration["Auth0:Audience"] ?? 
                       throw new InvalidOperationException("Auth0 Audience is not configured");
                       
            _issuer = $"https://{_domain}/";
                
            // Set up JWKS discovery for Auth0
            var metadataAddress = $"{_issuer}.well-known/openid-configuration";
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());
                
            // Preload the configuration
            _ = _configurationManager.GetConfigurationAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Validates a JWT token using the Auth0 JWKS
        /// </summary>
        public async Task<(bool isValid, ClaimsPrincipal? principal, string? error)> ValidateTokenAsync(string token)
        {
            try
            {
                var config = await _configurationManager.GetConfigurationAsync(CancellationToken.None);
                
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = config.SigningKeys,
                    NameClaimType = "name",
                    RoleClaimType = "https://babbly.com/roles",
                    ClockSkew = TimeSpan.FromMinutes(5) // Allow a 5-minute clock skew
                };
                
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                
                return (true, principal, null);
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token has expired");
                return (false, null, "Token has expired");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                _logger.LogWarning("Token signature is invalid");
                return (false, null, "Invalid token signature");
            }
            catch (SecurityTokenInvalidAudienceException)
            {
                _logger.LogWarning("Token audience is invalid");
                return (false, null, "Invalid token audience");
            }
            catch (SecurityTokenInvalidIssuerException)
            {
                _logger.LogWarning("Token issuer is invalid");
                return (false, null, "Invalid token issuer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed");
                return (false, null, $"Token validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets payload from token without validation for debugging purposes
        /// </summary>
        public (bool isValid, IDictionary<string, object>? payload, string? error) ExtractTokenPayload(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                
                var payload = new Dictionary<string, object>();
                foreach (var claim in jwtToken.Claims)
                {
                    payload[claim.Type] = claim.Value;
                }
                
                // Add token properties - Convert DateTime to Unix timestamp (seconds since epoch)
                payload["exp"] = new DateTimeOffset(jwtToken.ValidTo).ToUnixTimeSeconds();
                payload["iat"] = new DateTimeOffset(jwtToken.IssuedAt).ToUnixTimeSeconds();
                
                return (true, payload, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting token payload");
                return (false, null, $"Error extracting token payload: {ex.Message}");
            }
        }

        public string? GetUserIdFromToken(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? user.FindFirst("sub")?.Value;
        }

        /// <summary>
        /// Determines if a user is authorized to access a specific resource for the given operation
        /// </summary>
        public async Task<bool> IsAuthorizedForResourceAsync(string userId, List<string> roles, string resourcePath, string operation)
        {
            // Create a cache key
            string cacheKey = $"{userId}:{string.Join(",", roles)}:{resourcePath}:{operation}";
            
            // Check cache first
            if (_authorizationCache.TryGetValue(cacheKey, out bool cachedResult))
            {
                return cachedResult;
            }
            
            // Implement authorization logic here
            // This is a simple role-based authorization example
            bool isAuthorized = false;
            
            // Public resources that don't require authorization
            if (resourcePath.StartsWith("/api/health") || 
                resourcePath.StartsWith("/api/auth/health"))
            {
                isAuthorized = true;
            }
            // Admin-only resources
            else if (resourcePath.StartsWith("/api/admin"))
            {
                isAuthorized = roles.Contains("admin");
            }
            // User resources - require authentication
            else if (resourcePath.StartsWith("/api/users"))
            {
                if (operation == "GET" || operation == "POST")
                {
                    // Anyone can GET user info or create users (for signup)
                    isAuthorized = true;
                }
                else if (resourcePath.Contains($"/api/users/{userId}"))
                {
                    // Users can modify their own resources
                    isAuthorized = true;
                }
                else
                {
                    // For other operations, require admin privileges
                    isAuthorized = roles.Contains("admin");
                }
            }
            // Post resources
            else if (resourcePath.StartsWith("/api/posts"))
            {
                if (operation == "GET")
                {
                    // Anyone can read posts
                    isAuthorized = true;
                }
                else
                {
                    // Must be authenticated to create/update/delete posts
                    isAuthorized = !string.IsNullOrEmpty(userId);
                }
            }
            // Comment resources
            else if (resourcePath.StartsWith("/api/comments"))
            {
                if (operation == "GET")
                {
                    // Anyone can read comments
                    isAuthorized = true;
                }
                else
                {
                    // Must be authenticated to create/update/delete comments
                    isAuthorized = !string.IsNullOrEmpty(userId);
                }
            }
            // Like resources
            else if (resourcePath.StartsWith("/api/likes"))
            {
                if (operation == "GET")
                {
                    // Anyone can read likes
                    isAuthorized = true;
                }
                else
                {
                    // Must be authenticated to create/update/delete likes
                    isAuthorized = !string.IsNullOrEmpty(userId);
                }
            }
            
            // Cache the result (with a reasonable expiration if implemented)
            _authorizationCache.TryAdd(cacheKey, isAuthorized);
            
            return isAuthorized;
        }
    }
} 