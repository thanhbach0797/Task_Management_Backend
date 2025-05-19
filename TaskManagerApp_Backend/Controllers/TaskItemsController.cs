using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskManagerApp_Backend.Data;
using TaskManagerApp_Backend.Hubs;
using TaskManagerApp_Backend.Models;

namespace TaskManagerApp_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskItemsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<TaskHub> _hubContext;

        public TaskItemsController(ApplicationDbContext context, IHubContext<TaskHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET /api/TaskItems
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? projectId, int? status, int? userId)
        {
            var query = _context.TaskItems
                .Include(t => t.Project)
                .AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(t => t.AssignedTo == userId.Value);
            }
            // Apply sorting
            query = query.OrderByDescending(t => t.Id);

            // Tải dữ liệu về phía client
            var tasks = await query.ToListAsync();

            // Thu thập tất cả AssignedTo IDs từ TaskItems
            var allAssignedToIds = query.Select(t => t.AssignedTo).Distinct().ToList();

            // Tải chỉ các employees cần thiết
            var employees = await _context.Employees
                .Where(e => allAssignedToIds.Contains(e.Id))
                .ToListAsync();

            // Chuyển đổi sang TaskItemDto
            var taskDtos = tasks.Select(t => new TaskItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                AssignedTo = t.AssignedTo,
                IsCompleted = t.IsCompleted,
                ProjectId = t.ProjectId,
                ProjectName = t.Project != null ? t.Project.Name : "",
                Status = t.Status,
                DueDate = t.DueDate,
                AssignedToName = employees.Where(e => e.Id == t.AssignedTo).Select(e => e.Username).FirstOrDefault() ?? ""
            }).ToList();

            return Ok(taskDtos);
        }

        // POST /api/TaskItems
        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskItem taskItem)
        {
            if (taskItem == null)
                return BadRequest();

            _context.TaskItems.Add(taskItem);
            await _context.SaveChangesAsync();

            // Tải thông tin employee để lấy userId (giả sử dùng Username hoặc Email)
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == taskItem.AssignedTo);

            if (employee != null)
            {
                string assignedUserId = employee.Id.ToString() ?? taskItem.AssignedTo.ToString();
                await _hubContext.Clients.User(assignedUserId)
                    .SendAsync("TaskAssigned", new
                    {
                        taskItem.Title
                    });
            }

            return CreatedAtAction(nameof(GetById), new { id = taskItem.Id }, taskItem);
        }

        // GET /api/TaskItems/{id}
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var taskItem = await _context.TaskItems
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (taskItem == null)
                return NotFound(new { Message = $"Task with ID {id} not found." });

            // Tải employee cho AssignedTo
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == taskItem.AssignedTo);

            // Chuyển đổi sang TaskItemDto
            var taskDto = new TaskItemDto
            {
                Id = taskItem.Id,
                Title = taskItem.Title,
                Description = taskItem.Description,
                AssignedTo = taskItem.AssignedTo,
                IsCompleted = taskItem.IsCompleted,
                ProjectId = taskItem.ProjectId,
                ProjectName = taskItem.Project != null ? taskItem.Project.Name : "",
                Status = taskItem.Status,
                DueDate = taskItem.DueDate,
                AssignedToName = employee?.Username ?? ""
            };

            return Ok(taskDto);
        }

        /// <summary>
        /// Retrieves tasks filtered by a list of project IDs and an optional assigned user ID.
        /// </summary>
        /// <param name="projectIds">List of project IDs to filter tasks. Returns empty list if empty.</param>
        /// <param name="assignedTo">Optional user ID to filter tasks assigned to a specific user.</param>
        //[Authorize]
        [HttpGet("filter")]
        public async Task<IActionResult> GetFilteredTasks([FromQuery] List<int> projectIds, [FromQuery] int? assignedTo, [FromQuery] int? status)
        {
            // Return empty list if projectIds is empty
            if (!projectIds.Any())
                return Ok(new List<TaskItemDto>());

            // Build query for tasks
            var query = _context.TaskItems
                .Include(t => t.Project)
                .Where(t => projectIds.Contains(t.ProjectId))
                .AsQueryable();

            // Apply AssignedTo filter if provided
            if (assignedTo.HasValue)
            {
                query = query.Where(t => t.AssignedTo == assignedTo.Value);
            }

            // Apply AssignedTo filter if provided
            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            // Apply sorting
            query = query.OrderByDescending(t => t.Id);

            // Load tasks
            var tasks = await query.ToListAsync();

            // Collect all AssignedTo IDs from tasks
            var allAssignedToIds = tasks.Select(t => t.AssignedTo).Distinct().ToList();

            // Load only the necessary employees
            var employees = await _context.Employees
                .Where(e => allAssignedToIds.Contains(e.Id))
                .ToListAsync();

            // Convert to TaskItemDto
            var taskDtos = tasks.Select(t => new TaskItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                AssignedTo = t.AssignedTo,
                IsCompleted = t.IsCompleted,
                ProjectId = t.ProjectId,
                ProjectName = t.Project != null ? t.Project.Name : "",
                Status = t.Status,
                DueDate = t.DueDate,
                AssignedToName = employees.FirstOrDefault(e => e.Id == t.AssignedTo)?.Username ?? ""
            }).ToList();

            return Ok(taskDtos);
        }

        // PUT /api/TaskItems/{id}
        //[Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TaskItem updatedTask)
        {
            if (id != updatedTask.Id)
                return BadRequest();

            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem == null)
                return NotFound();

            // Tải thông tin employee
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == taskItem.AssignedTo);

            if (employee != null)
            {
                if(taskItem.Id != updatedTask.AssignedTo)
                {
                    string assignedUserId = updatedTask.AssignedTo.ToString();
                    await _hubContext.Clients.User(assignedUserId)
                        .SendAsync("TaskAssigned", new
                        {
                            taskItem.Title
                        });
                }
            }

            // Cập nhật các trường
            taskItem.Title = updatedTask.Title;
            taskItem.Description = updatedTask.Description;
            taskItem.Status = updatedTask.Status;
            taskItem.DueDate = updatedTask.DueDate;
            taskItem.ProjectId = updatedTask.ProjectId;
            taskItem.AssignedTo = updatedTask.AssignedTo;

            _context.TaskItems.Update(taskItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /api/TaskItems/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem == null)
                return NotFound();

            _context.TaskItems.Remove(taskItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
