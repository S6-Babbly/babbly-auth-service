using babbly_auth_service.Models;
using babbly_auth_service.Models.Dtos;
using System.Security.Claims;

namespace babbly_auth_service.Services
{
    public class UserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly KafkaProducerService _kafkaProducer;

        public UserService(ILogger<UserService> logger, KafkaProducerService kafkaProducer)
        {
            _logger = logger;
            _kafkaProducer = kafkaProducer;
        }

        // Extract user data from a token's claims
        public UserResponse GetUserFromClaims(ClaimsPrincipal user)
        {
            try
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
                var email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
                var name = user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst("name")?.Value;
                var picture = user.FindFirst("picture")?.Value;
                var emailVerified = bool.TryParse(user.FindFirst("email_verified")?.Value, out bool verified) ? verified : false;
                var locale = user.FindFirst("locale")?.Value;

                // Extract roles (if present)
                var rolesClaim = user.FindFirst("https://babbly.com/roles")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value;
                string[]? roles = null;
                
                if (!string.IsNullOrEmpty(rolesClaim))
                {
                    roles = rolesClaim.Split(',', StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    // Look for individual role claims
                    var roleClaims = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
                    if (roleClaims.Any())
                    {
                        roles = roleClaims;
                    }
                }

                return new UserResponse
                {
                    Id = userId ?? string.Empty,
                    Email = email ?? string.Empty,
                    Name = name,
                    Picture = picture,
                    CreatedAt = DateTime.UtcNow, // Not tracked anymore
                    UpdatedAt = DateTime.UtcNow, // Not tracked anymore
                    EmailVerified = emailVerified,
                    Locale = locale,
                    Roles = roles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user data from claims");
                throw;
            }
        }

        // Map incoming user data to a response and publish event to Kafka
        public async Task<UserResponse> SyncUserAsync(SyncUserRequest request, bool isNewUser = false)
        {
            try {
                // Create user object
                var user = new User
                {
                    Id = Guid.NewGuid().ToString(), // Generate new user ID
                    Auth0Id = request.Auth0Id,
                    Email = request.Email,
                    Name = request.Name,
                    Picture = request.Picture,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    EmailVerified = request.EmailVerified,
                    Locale = request.Locale,
                    Roles = request.Roles
                };

                // Publish event to Kafka
                if (isNewUser)
                {
                    await _kafkaProducer.PublishUserCreatedEventAsync(user);
                }
                else
                {
                    await _kafkaProducer.PublishUserUpdatedEventAsync(user);
                }

                // Return user response
                return new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Picture = user.Picture,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    EmailVerified = user.EmailVerified,
                    Locale = user.Locale,
                    Roles = user.Roles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user");
                throw;
            }
        }
    }
} 