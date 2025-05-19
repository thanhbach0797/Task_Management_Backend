using Microsoft.EntityFrameworkCore;
using TaskManagerApp_Backend.Models;

namespace TaskManagerApp_Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Project> Projects { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Employee> Employees { get; set; }
    }
}
