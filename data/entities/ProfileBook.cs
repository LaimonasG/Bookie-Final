using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Bakalauras.data.entities
{
    public class ProfileBook
    {
        [Key]
        [Column(Order = 0)]
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
        [Key]
        [Column(Order = 1)]
        public int BookId { get; set; }
        public Book Book { get; set; }
        [Key]
        [Column(Order = 3)]
        public bool WasUnsubscribed { get; set; }
        public string? BoughtChapterList { get; set; }
    }
}
