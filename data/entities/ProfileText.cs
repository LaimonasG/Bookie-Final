namespace Bakalauras.data.entities
{
    public class ProfileText
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
        public int TextId { get; set; }
        public Text Text { get; set; }
    }
}
