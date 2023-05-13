using Bakalauras.Auth;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bakalauras.data.entities
{
    public class Chapter : IUserOwnedResource
    {
        public int Id { get; set; }
        [Required]
        public int BookId { get; set; }

        [Required]
        public string UserId { get; set; }

        public string Name { get; set; }

        [Column(TypeName = "text")]
        public string Content { get; set; }

        public virtual ICollection<Comment>? Comments { get; set; }
    }
}
