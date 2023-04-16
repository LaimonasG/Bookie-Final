using Bakalauras.Auth;
using Bakalauras.Auth.Model;
using Microsoft.Build.Framework;

namespace Bakalauras.data.entities
{
    public class Profile : IUserOwnedResource
    {
        public int Id { get; set; }
        public double Points { get; set; }
        public byte[]? ProfilePicture { get; set; }
        public string? TextPurchaseDates { get; set; }

        public ICollection<ProfileBook>? ProfileBooks { get; set; }
        public ICollection<ProfileText>? ProfileTexts { get; set; }
        public ICollection<DailyQuestionProfile>? DailyQuestionProfiles { get; set; }
        public ICollection<PaymentUser>? PaymentUser { get; set; }
        [Required]
        public string UserId { get; set; } 
        public BookieUser User { get; set; }
    }
}
