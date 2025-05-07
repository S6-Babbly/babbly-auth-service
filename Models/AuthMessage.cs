using System.Text.Json.Serialization;

namespace babbly_auth_service.Models
{
    /// <summary>
    /// Message containing authorization information for inter-service communication
    /// </summary>
    public class AuthMessage
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;
        
        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; } = new List<string>();
        
        [JsonPropertyName("is_authenticated")]
        public bool IsAuthenticated { get; set; }
        
        [JsonPropertyName("resource_path")]
        public string ResourcePath { get; set; } = string.Empty;
        
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty; // e.g., "READ", "WRITE", "DELETE"
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [JsonPropertyName("correlation_id")]
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        
        [JsonPropertyName("is_authorized")]
        public bool IsAuthorized { get; set; }
    }
} 