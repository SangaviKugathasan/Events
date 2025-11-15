using Microsoft.AspNetCore.Identity;

namespace EventZax.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsApproved { get; set; }
    }
}
