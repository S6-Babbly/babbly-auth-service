using babbly_auth_service.Models.Dtos;
using babbly_auth_service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace babbly_auth_service.Controllers
{
    [Route("auth")]
    [ApiController]
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
        /// Validates an Auth0 JWT token and returns its decoded payload
        /// </summary>
        /// <param name="request">Request containing the JWT token</param>
        /// <returns>Validation result and decoded token payload</returns>
        [HttpPost("validate-token")]
        public IActionResult ValidateToken([FromBody] ValidateTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new ValidateTokenResponse
                {
                    Valid = false,
                    Error = "Token is required"
                });
            }

            var (isValid, payload, error) = _tokenService.ValidateToken(request.Token);

            return Ok(new ValidateTokenResponse
            {
                Valid = isValid,
                Payload = payload,
                Error = error
            });
        }

        /// <summary>
        /// Syncs Auth0 user information without persisting
        /// </summary>
        /// <param name="request">User information from Auth0</param>
        /// <returns>Processed user information</returns>
        [HttpPost("sync-user")]
        public IActionResult SyncUser([FromBody] SyncUserRequest request)
        {
            if (string.IsNullOrEmpty(request.Auth0Id) || string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Auth0Id and Email are required");
            }

            try
            {
                // Simply transform the request to a response without database interaction
                var userResponse = _userService.MapSyncRequestToResponse(request);
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user data");
                return StatusCode(500, "Error processing user data");
            }
        }

        /// <summary>
        /// Returns the authenticated user's information from the token
        /// </summary>
        /// <returns>User information extracted from token</returns>
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetMe()
        {
            try
            {
                // Extract user info directly from the token claims
                var userResponse = _userService.GetUserFromClaims(User);
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user data from token");
                return StatusCode(500, "Error extracting user data from token");
            }
        }
    }
} 