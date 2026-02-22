using System.ComponentModel.DataAnnotations;

namespace PddTrainer.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; }
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
