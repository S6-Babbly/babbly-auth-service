using System.Text.Json.Serialization;

namespace babbly_auth_service.Models.Dtos
{
    // DTO for token validation request
    public class ValidateTokenRequest
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    // DTO for token validation response
    public class ValidateTokenResponse
    {
        [JsonPropertyName("valid")]
        public bool Valid { get; set; }

        [JsonPropertyName("payload")]
        public Dictionary<string, object>? Payload { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    // DTO for user sync from Auth0
    public class SyncUserRequest
    {
        [JsonPropertyName("auth0_id")]
        public string Auth0Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }

        [JsonPropertyName("roles")]
        public string[]? Roles { get; set; }
    }

    // DTO for user response
    public class UserResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }

        [JsonPropertyName("roles")]
        public string[]? Roles { get; set; }
    }
} 