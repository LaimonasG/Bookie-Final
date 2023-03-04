using Bakalauras.Auth.Model;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Bakalauras.data.entities
{
    public class Comment
    {
        public int Id { get; set; }

        public int BookId { get; set; }

        public DateTime Date { get; set; }

        public string Content { get; set; }

        public string Type { get; set; }

        [Required]
        public string UserId { get; set; }
        public string Username { get; set; }

        public Chapter Chapter { get; set; }

        public Book Book { get; set; }

        public Text Text { get; set; }

         public BookieUser User { get; set; }

    }
}
