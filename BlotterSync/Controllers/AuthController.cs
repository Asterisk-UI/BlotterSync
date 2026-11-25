using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BlotterSync.DTOs;
using BlotterSync.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BlotterSync.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly BlotterSyncContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher<Officer> _passwordHasher;

        public AuthController(
            BlotterSyncContext context,
            IConfiguration configuration,
            IPasswordHasher<Officer> passwordHasher)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            var user = await _context.Officers.FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null || user.ActiveStatus != true || !await VerifyPasswordAsync(user, loginDto.Password))
            {
                return Unauthorized("Invalid username or password.");
            }

            return CreateLoginResponse(user);
        }

        [HttpPost("Demo")]
        public async Task<IActionResult> Demo([FromServices] IWebHostEnvironment environment)
        {
            if (!environment.IsDevelopment() || !_configuration.GetValue<bool>("Demo:Enabled")) return NotFound();
            var user = await _context.Officers.SingleOrDefaultAsync(o => o.Username == "demo.officer");
            return user is null ? NotFound() : CreateLoginResponse(user);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("Me")]
        public async Task<IActionResult> Me([FromServices] IWebHostEnvironment environment)
        {
            var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Officers.FindAsync(id);
            return user is null ? Unauthorized() : Ok(new { name = $"{user.FirstName} {user.LastName}", user.Role,
                demo = environment.IsDevelopment() && _configuration.GetValue<bool>("Demo:Enabled") });
        }

        private IActionResult CreateLoginResponse(Officer user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.OfficerId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "JWT key configuration is missing.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                role = user.Role,
                officerId = user.OfficerId,
                name = $"{user.FirstName} {user.LastName}"
            });
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
        {
            if (await _context.Officers.AnyAsync(u => u.Username == registerDto.Username))
            {
                return BadRequest("Username already exists.");
            }

            if (await _context.Officers.AnyAsync(u => u.BadgeNumber == registerDto.BadgeNumber))
            {
                return BadRequest("Badge number is already registered.");
            }

            var newOfficer = new Officer
            {
                BadgeNumber = registerDto.BadgeNumber,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Username = registerDto.Username,
                Role = "Officer",
                ActiveStatus = true
            };

            newOfficer.PasswordHash = _passwordHasher.HashPassword(newOfficer, registerDto.Password);

            _context.Officers.Add(newOfficer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful. You can now log in." });
        }

        private async Task<bool> VerifyPasswordAsync(Officer user, string password)
        {
            PasswordVerificationResult result;

            if (IsIdentityHash(user.PasswordHash))
            {
                result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            }
            else
            {
                result = IsLegacyPlaintextMatch(user.PasswordHash, password)
                    ? PasswordVerificationResult.SuccessRehashNeeded
                    : PasswordVerificationResult.Failed;
            }

            if (result == PasswordVerificationResult.Failed)
            {
                return false;
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        private static bool IsIdentityHash(string stored)
        {
            var buffer = new byte[stored.Length];
            return Convert.TryFromBase64String(stored, buffer, out var length)
                && length >= 49
                && (buffer[0] == 0x00 || buffer[0] == 0x01);
        }

        private static bool IsLegacyPlaintextMatch(string stored, string password)
        {
            var storedBytes = Encoding.UTF8.GetBytes(stored);
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            return CryptographicOperations.FixedTimeEquals(storedBytes, passwordBytes);
        }
    }
}