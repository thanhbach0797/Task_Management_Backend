namespace TaskManagerApp_Backend.Models
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Members { get; set; } = "";
        public string MemberIds { get; set; } = "";
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}