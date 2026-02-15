using System.ComponentModel.DataAnnotations;

namespace BaseApiNet10.Core.DTOs.Auth
{
    public class UserForggotPasswordRequestDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;
    }
}
