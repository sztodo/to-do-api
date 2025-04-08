using To_Do_App_API.Controllers.DTOs;

namespace To_Do_App_API.Application.Interfaces.IServices
{
    public interface IAuthService
    {
        Task<(bool Success, string Token, UserDto User)> Login(LoginDto loginDto);
        Task<(bool Success, string Message)> Register(RegisterDto registerDto);
    }
}
