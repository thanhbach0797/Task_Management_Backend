using System.ComponentModel.DataAnnotations;

namespace TaskManagerApp_Backend.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Username { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [Required]
        [StringLength(50)]
        public required string Role { get; set; }
    }
}
