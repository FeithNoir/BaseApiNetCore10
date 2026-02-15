using System.ComponentModel.DataAnnotations;

namespace BaseApiNet10.Core.DTOs.Auth
{
    public class UserLoginRequestDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
