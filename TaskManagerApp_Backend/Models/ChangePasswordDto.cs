namespace TaskManagerApp_Backend.Models
{
    public class ChangePasswordDto
    {
        public string Username { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}