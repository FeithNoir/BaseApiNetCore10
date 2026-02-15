using BaseApiNet10.Business.Services;
using BaseApiNet10.Core.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BaseApiNet10.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly AuthService _authService;
        private string baseUrl = "";

        public AuthenticationController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto request)
        {
            var result = await _authService.RegisterAsync(request, baseUrl);
            return result.Result ? Ok(result) : BadRequest(result);
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] UserForggotPasswordRequestDto request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            return result.Result ? Ok(result) : BadRequest(result);
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto model)
        {
            var result = await _authService.ResetPasswordAsync(model);
            return result.Result ? Ok(result) : BadRequest(result);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            return result.Result ? Ok(result) : BadRequest(result);
        }

        [HttpPost("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto confirmEmailDto)
        {
            var result = await _authService.ConfirmEmailAsync(confirmEmailDto.UserId, confirmEmailDto.Code);
            if (result.Errors.Count > 0)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
