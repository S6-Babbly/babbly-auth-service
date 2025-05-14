using System.Text.Json.Serialization;

namespace babbly_auth_service.Models
{
    /// <summary>
    /// Base class for Kafka events
    /// </summary>
    public abstract class KafkaEvent
    {
        [JsonPropertyName("event_id")]
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        
        [JsonPropertyName("event_type")]
        public string EventType { get; set; } = string.Empty;
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";
    }
    
    /// <summary>
    /// Event published when a user is created
    /// </summary>
    public class UserCreatedEvent : KafkaEvent
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;
        
        [JsonPropertyName("auth0_id")]
        public string Auth0Id { get; set; } = string.Empty;
        
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
        
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Event published when a user is updated
    /// </summary>
    public class UserUpdatedEvent : KafkaEvent
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;
        
        [JsonPropertyName("auth0_id")]
        public string Auth0Id { get; set; } = string.Empty;
        
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
        
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 