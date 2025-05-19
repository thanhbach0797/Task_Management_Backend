using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagerApp_Backend.Data;
using TaskManagerApp_Backend.Models;

namespace TaskManagerApp_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Projects
        /// <summary>
        /// Retrieves all projects with their associated tasks and member names.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Tải tất cả projects và tasks
            var projects = await _context.Projects
                .Include(p => p.Tasks)
                .ToListAsync();

            // Thu thập tất cả ID từ Members
            var allMemberIds = projects
                .SelectMany(p => p.Members.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim())
                    .Where(id => int.TryParse(id, out _))
                    .Select(int.Parse))
                .Distinct()
                .ToList();

            // Tải chỉ các employees cần thiết
            var employees = await _context.Employees
                .Where(e => allMemberIds.Contains(e.Id))
                .ToListAsync();

            // Chuyển đổi sang Dto
            var projectDtos = projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Members = GetMemberNames(p.Members, employees),
                MemberIds = p.Members,
                Tasks = p.Tasks
            }).ToList();

            return Ok(projectDtos);
        }

        // GET: api/Projects/{id}
        /// <summary>
        /// Retrieves a project by ID with its associated tasks and member names.
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound(new { Message = $"Project with ID {id} not found." });

            // Thu thập ID từ Members
            var memberIds = project.Members
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim())
                .Where(id => int.TryParse(id, out _))
                .Select(int.Parse)
                .ToList();

            // Tải chỉ các employees cần thiết
            var employees = await _context.Employees
                .Where(e => memberIds.Contains(e.Id))
                .ToListAsync();

            var projectDto = new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Members = GetMemberNames(project.Members, employees),
                MemberIds = project.Members,
                Tasks = project.Tasks
            };

            return Ok(projectDto);
        }

        // GET: api/Projects/{id}/Employees
        /// <summary>
        /// Retrieves all employees associated with a project by project ID.
        /// </summary>
        [Authorize]
        [HttpGet("{id}/Employees")]
        public async Task<IActionResult> GetEmployeesByProjectId(int id)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound(new { Message = $"Project with ID {id} not found." });

            // Thu thập ID từ Members
            var memberIds = project.Members
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim())
                .Where(id => int.TryParse(id, out _))
                .Select(int.Parse)
                .ToList();

            if (!memberIds.Any())
                return Ok(new List<Employee>());

            // Tải danh sách employees theo memberIds
            var employees = await _context.Employees
                .Where(e => memberIds.Contains(e.Id))
                .ToListAsync();

            return Ok(employees);
        }

        /// <summary>
        /// Retrieves all projects that a specific user is participating in, with their associated tasks and member names.
        /// </summary>
        /// <param name="userId">The ID of the user to filter projects by.</param>
        //[Authorize]
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetProjectsByUserId(int userId)
        {
            // Load all projects and their tasks
            var projects = await _context.Projects
                .Include(p => p.Tasks)
                .ToListAsync();

            // Filter projects where the userId is in the Members field
            var filteredProjects = projects
                .Where(p => !string.IsNullOrEmpty(p.Members) &&
                            p.Members.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(id => id.Trim())
                                .Where(id => int.TryParse(id, out _))
                                .Select(int.Parse)
                                .Contains(userId))
                .ToList();

            if (!filteredProjects.Any())
                return Ok(new List<ProjectDto>());

            // Collect all member IDs from filtered projects
            var allMemberIds = filteredProjects
                .SelectMany(p => p.Members.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim())
                    .Where(id => int.TryParse(id, out _))
                    .Select(int.Parse))
                .Distinct()
                .ToList();

            // Load only the necessary employees
            var employees = await _context.Employees
                .Where(e => allMemberIds.Contains(e.Id))
                .ToListAsync();

            // Convert to DTO
            var projectDtos = filteredProjects.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Members = GetMemberNames(p.Members, employees),
                MemberIds = p.Members,
                Tasks = p.Tasks
            }).ToList();

            return Ok(projectDtos);
        }

        // POST: api/Projects
        /// <summary>
        /// Creates a new project.
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Project project)
        {
            if (project == null || !ModelState.IsValid)
                return BadRequest(new { Message = "Invalid project data." });

            try
            {
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Message = "Error creating project.", Detail = ex.InnerException?.Message });
            }
        }

        // PUT: api/Projects/{id}
        /// <summary>
        /// Updates an existing project.
        /// </summary>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Project updatedProject)
        {
            if (id != updatedProject.Id || !ModelState.IsValid)
                return BadRequest(new { Message = "Invalid project data or ID mismatch." });

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { Message = $"Project with ID {id} not found." });

            try
            {
                project.Name = updatedProject.Name;
                project.StartDate = updatedProject.StartDate;
                project.EndDate = updatedProject.EndDate;
                project.Members = updatedProject.Members;

                _context.Projects.Update(project);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Message = "Error updating project.", Detail = ex.InnerException?.Message });
            }
        }

        // DELETE: api/Projects/{id}
        /// <summary>
        /// Deletes a project by ID.
        /// </summary>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { Message = $"Project with ID {id} not found." });

            try
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Message = "Error deleting project.", Detail = ex.InnerException?.Message });
            }
        }

        private string GetMemberNames(string memberIds, List<Employee> employees)
        {
            if (string.IsNullOrEmpty(memberIds))
                return "";

            // Tách chuỗi ID thành mảng
            var ids = memberIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim())
                .Where(id => int.TryParse(id, out _))
                .Select(int.Parse)
                .ToList();

            if (!ids.Any())
                return "";

            // Ánh xạ ID sang tên từ danh sách employees
            var names = employees
                .Where(e => ids.Contains(e.Id))
                .OrderBy(e => ids.IndexOf(e.Id))
                .Select(e => e.Username)
                .ToList();

            return string.Join(", ", names);
        }
    }
}