using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BaseApiNet10.Core.Entities.Models
{
    public class User : IdentityUser
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Charge { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
    }
}
