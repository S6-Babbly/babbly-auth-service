using System;
using System.ComponentModel.DataAnnotations;

namespace babbly_auth_service.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
        
        public string? Name { get; set; }
        
        public string? Picture { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Auth0 metadata and additional fields
        public string? Auth0Id { get; set; }
        
        public string? Locale { get; set; }
        
        public bool EmailVerified { get; set; }
        
        public string[]? Roles { get; set; }
    }
} 