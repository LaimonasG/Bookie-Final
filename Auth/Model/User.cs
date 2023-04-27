using Microsoft.AspNetCore.Identity;

namespace Bakalauras.Auth.Model
{
    public class BookieUser : IdentityUser
    {
        public bool isBlocked { get; set; }

        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
