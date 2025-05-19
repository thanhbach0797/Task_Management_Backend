using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TaskManagerApp_Backend.Data;
using TaskManagerApp_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TaskManagerApp_Backend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Employee> _passwordHasher;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Employee>();
        }

        // GET /api/Employee
        [Authorize]
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? role = null)
        {
            try
            {
                // Truy vấn cơ bản lấy thông tin nhân viên
                var query = _context.Employees.AsQueryable();

                // Lọc theo role nếu có
                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(e => e.Role == role);
                }

                // Tải dữ liệu từ database
                var employees = await query
                    .Select(e => new
                    {
                        e.Id,
                        e.Username,
                        e.Role
                    })
                    .ToListAsync();

                return Ok(employees);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // GET /api/Employee/{id}
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound();

            return Ok(new
            {
                employee.Id,
                employee.Username,
                employee.Role
            });
        }

        // Fix for CS9035: Required member 'Employee.PasswordHash' must be set in the object initializer or attribute constructor.
        // The issue occurs because the `PasswordHash` property is marked as `required` and must be initialized in the object initializer.

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployeeCreateDto dto)
        {
            if (await _context.Employees.AnyAsync(e => e.Username == dto.Username))
                return BadRequest("Username đã tồn tại");

            // Initialize the required 'PasswordHash' property in the object initializer
            var employee = new Employee
            {
                Username = dto.Username,
                Role = dto.Role,
                PasswordHash = _passwordHasher.HashPassword(null, dto.Password) // Set PasswordHash here
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, new
            {
                employee.Id,
                employee.Username,
                employee.Role
            });
        }

        // PUT /api/Employee/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployeeUpdateDto dto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                employee.PasswordHash = _passwordHasher.HashPassword(employee, dto.NewPassword);
            }

            if (!string.IsNullOrEmpty(dto.Role))
            {
                employee.Role = dto.Role;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /api/Employee/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
