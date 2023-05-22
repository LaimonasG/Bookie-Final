using Bakalauras.Auth;
using Bakalauras.Auth.Model;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bakalauras.data.entities
{
    public class Text : IUserOwnedResource
    {
        public int Id { get; set; }
        public string GenreName { get; set; }
        [Required]
        public string UserId { get; set; }

        public string Name { get; set; }

        public double Price { get; set; }

        public string Content { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }

        public string CoverImageUrl { get; set; }

        public virtual ICollection<Comment>? Comments { get; set; }

        public DateTime Created { get; set; }

        public Status Status { get; set; }

        public string? StatusComment { get; set; }

        public BookieUser User { get; set; }
    }
}
