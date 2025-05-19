using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagerApp_Backend.Data;
using TaskManagerApp_Backend.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManagerApp_Backend.Hubs;


namespace TaskManagerApp_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<Employee> _passwordHasher;
        private readonly IConfiguration _config;

        private static readonly Dictionary<string, (int Count, DateTime LastAttempt)> _loginAttempts = new();
        private const int MaxAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);


        public AuthController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Employee>();
            _config = config;
        }

        // POST: /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (_loginAttempts.TryGetValue(dto.Username, out var attemptInfo))
            {
                if (attemptInfo.Count >= MaxAttempts && DateTime.UtcNow - attemptInfo.LastAttempt < LockoutDuration)
                    return BadRequest("Tài khoản bị tạm khóa do nhập sai nhiều lần. Vui lòng thử lại sau vài phút.");
            }

            var user = await _context.Employees
                .FirstOrDefaultAsync(e => e.Username == dto.Username);

            if (user == null)
                return Unauthorized("Tài khoản không tồn tại");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                if (_loginAttempts.ContainsKey(dto.Username))
                    _loginAttempts[dto.Username] = (attemptInfo.Count + 1, DateTime.UtcNow);
                else
                    _loginAttempts[dto.Username] = (1, DateTime.UtcNow);

                return Unauthorized("Sai mật khẩu");
            }

            if (_loginAttempts.ContainsKey(dto.Username))
                _loginAttempts.Remove(dto.Username);

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Token = token,
                User = new { user.Id, user.Username, user.Role }
            });
        }


        // POST: /api/auth/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = await _context.Employees
                .FirstOrDefaultAsync(e => e.Username == dto.Username);

            if (user == null)
                return NotFound("Tài khoản không tồn tại");

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);

            if (verifyResult == PasswordVerificationResult.Failed)
                return BadRequest("Mật khẩu hiện tại không đúng");

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok("Đổi mật khẩu thành công");
        }

        private string GenerateJwtToken(Employee user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
