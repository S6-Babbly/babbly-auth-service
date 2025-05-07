using babbly_auth_service.Models;
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
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            TokenService tokenService,
            ILogger<AuthController> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpGet("validate")]
        public IActionResult ValidateToken()
        {
            // If the request made it to this point, the token is valid
            // (because of the JWT authentication middleware)
            return Ok(new { isValid = true });
        }
        
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy" });
        }

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

        [HttpGet("userinfo")]
        [Authorize]
        public IActionResult GetUserInfo()
        {
            try
            {
                var userId = _tokenService.GetUserIdFromToken(User);
                var roles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role || c.Type == "https://babbly.com/roles")
                    .Select(c => c.Value)
                    .ToList();

                var claims = new Dictionary<string, string>();
                foreach (var claim in User.Claims)
                {
                    if (!claims.ContainsKey(claim.Type))
                    {
                        claims.Add(claim.Type, claim.Value);
                    }
                }

                return Ok(new 
                { 
                    userId, 
                    roles, 
                    isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    claims
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                return StatusCode(500, new { message = "Error getting user info" });
            }
        }
    }
} 