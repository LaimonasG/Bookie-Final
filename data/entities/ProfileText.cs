using System.Diagnostics.CodeAnalysis;

namespace Bakalauras.data.entities
{
    public class ProfileText
    {
        [AllowNull]
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
        [AllowNull]
        public int TextId { get; set; }
        public Text Text { get; set; }

        public DateTime? BoughtDate { get; set; }
    }
}
