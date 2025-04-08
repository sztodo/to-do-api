using Microsoft.AspNetCore.Mvc;
using To_Do_App_API.Application.Interfaces.IServices;
using To_Do_App_API.Controllers.DTOs;

namespace To_Do_App_API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var (success, message) = await _authService.Register(registerDto);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var (success, token, user) = await _authService.Login(loginDto);

            if (!success)
                return Unauthorized(new { message = "Invalid username or password" });

            return Ok(new { token, user });
        }
    }
}
