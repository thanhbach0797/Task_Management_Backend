namespace TaskManagerApp_Backend.Models
{
    public class TaskItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int AssignedTo { get; set; }
        public bool IsCompleted { get; set; } = false;

        // Khóa ngoại đến Project
        public int ProjectId { get; set; }

        // Navigation property
        public Project? Project { get; set; }

        public int Status { get; set; }
        public DateTime DueDate { get; set; }

        public string AssignedToName { get; set; }
        public string ProjectName { get; set; }
    }
}
