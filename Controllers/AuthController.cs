using babbly_auth_service.Models;
using babbly_auth_service.Models.Dtos;
using babbly_auth_service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace babbly_auth_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly TokenService _tokenService;
        private readonly UserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            TokenService tokenService,
            UserService userService,
            ILogger<AuthController> logger)
        {
            _tokenService = tokenService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy" });
        }

        /// <summary>
        /// Validate a JWT token
        /// </summary>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new ValidateTokenResponse 
                { 
                    Valid = false, 
                    Error = "Token is required" 
                });
            }

            try
            {
                var (isValid, principal, error) = await _tokenService.ValidateTokenAsync(request.Token);
                
                if (!isValid || principal == null)
                {
                    return Ok(new ValidateTokenResponse
                    {
                        Valid = false,
                        Error = error
                    });
                }
                
                // Extract payload for response
                var payload = new Dictionary<string, object>();
                foreach (var claim in principal.Claims)
                {
                    payload[claim.Type] = claim.Value;
                }
                
                return Ok(new ValidateTokenResponse
                {
                    Valid = true,
                    Payload = payload
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return StatusCode(500, new ValidateTokenResponse
                {
                    Valid = false,
                    Error = "Internal server error validating token"
                });
            }
        }

        /// <summary>
        /// Check if a user is authorized to access a resource
        /// </summary>
        [HttpGet("authorize")]
        [Authorize]
        public async Task<IActionResult> Authorize([FromQuery] string resourcePath, [FromQuery] string operation)
        {
            try
            {
                var userId = _tokenService.GetUserIdFromToken(User);
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Authorization denied: Could not extract user ID from token");
                    return StatusCode(403, new { isAuthorized = false, message = "User ID not found in token" });
                }
                
                var roles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role || c.Type == "https://babbly.com/roles")
                    .Select(c => c.Value)
                    .ToList();

                bool isAuthorized = await _tokenService.IsAuthorizedForResourceAsync(
                    userId, 
                    roles, 
                    resourcePath, 
                    operation);

                if (isAuthorized)
                {
                    return Ok(new { isAuthorized = true });
                }
                else
                {
                    return StatusCode(403, new { isAuthorized = false, message = "Not authorized for this resource/operation" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authorization check");
                return StatusCode(500, new { message = "Error processing authorization request" });
            }
        }

        /// <summary>
        /// Get user info from token
        /// </summary>
        [HttpGet("userinfo")]
        [Authorize]
        public IActionResult GetUserInfo()
        {
            try
            {
                var userResponse = _userService.GetUserFromClaims(User);
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                return StatusCode(500, new { message = "Error getting user info" });
            }
        }
        
        /// <summary>
        /// Sync user data from Auth0 and publish to Kafka
        /// </summary>
        [HttpPost("sync-user")]
        [Authorize]
        public async Task<IActionResult> SyncUser([FromBody] SyncUserRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Auth0Id))
                {
                    return BadRequest(new { message = "Auth0 ID is required" });
                }
                
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { message = "Email is required" });
                }
                
                // Check if current user is authorized to sync this user
                // Only allow users to sync their own data unless they're an admin
                var userId = _tokenService.GetUserIdFromToken(User);
                var isAdmin = User.Claims
                    .Any(c => (c.Type == ClaimTypes.Role || c.Type == "https://babbly.com/roles") && 
                              c.Value.Equals("admin", StringComparison.OrdinalIgnoreCase));
                              
                if (userId != request.Auth0Id && !isAdmin)
                {
                    return StatusCode(403, new { message = "Not authorized to sync this user" });
                }
                
                // Determine if this is a new user or existing user
                bool isNewUser = true; // In a real implementation, we'd check the database
                
                // Process user sync and publish to Kafka
                var userResponse = await _userService.SyncUserAsync(request, isNewUser);
                
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user");
                return StatusCode(500, new { message = "Error syncing user" });
            }
        }
    }
} 