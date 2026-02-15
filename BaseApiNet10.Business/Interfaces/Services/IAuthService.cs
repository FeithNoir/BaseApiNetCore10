using BaseApiNet10.Core.DTOs.Auth;
using BaseApiNet10.Core.Entities.Auth;

namespace BaseApiNet10.Business.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(UserRegistrationRequestDto request, string baseUrl);
        Task<AuthResult> ForgotPasswordAsync(UserForggotPasswordRequestDto request);
        Task<AuthResult> ResetPasswordAsync(ResetPasswordRequestDto model);
        Task<AuthResult> LoginAsync(UserLoginRequestDto dto);
        Task<AuthResult> ConfirmEmailAsync(string userId, string code);
    }
}
