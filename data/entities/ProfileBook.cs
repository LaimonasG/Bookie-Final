namespace Bakalauras.data.entities
{
    public class ProfileBook
    {
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
        public int BookId { get; set; }
        public Book Book { get; set; }
        public bool WasUnsubscribed { get; set; }
        public string? BoughtChapterList { get; set; }
        public DateTime? BoughtDate { get; set; }
    }
}
