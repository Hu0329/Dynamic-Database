using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyApi.Data;
using MyApi.Models;

namespace MyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("addUser")]
        public async Task<IActionResult> AddUser([FromBody] User user)
        {
            if (user == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User added successfully." });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User login)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == login.Username && u.Password == login.Password);
            if (user == null)
            {
                return Unauthorized();
            }
            return Ok(new { user.Id, user.Username, user.Content });
        }

        [HttpPut("updateUserContent")]
        public async Task<IActionResult> UpdateUserContent([FromBody] JsonElement contentData)
        {
            try
            {
                if (!contentData.TryGetProperty("userId", out JsonElement userIdElement) ||
                    !contentData.TryGetProperty("content", out JsonElement contentElement))
                {
                    return BadRequest(new { message = "Invalid request payload." });
                }

                int userId = userIdElement.GetInt32();
                string? content = contentElement.GetString();

                if (content == null)
                {
                    return BadRequest(new { message = "Content cannot be null." });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.Content = content;
                await _context.SaveChangesAsync();
                return Ok(new { message = "User content updated successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}