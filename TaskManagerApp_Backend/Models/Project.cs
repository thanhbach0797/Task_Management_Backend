using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManagerApp_Backend.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(500)]
        public string Members { get; set; } = ""; // Lưu trữ ID của Employee, phân tách bởi dấu phẩy

        // Navigation
        [JsonIgnore]
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}