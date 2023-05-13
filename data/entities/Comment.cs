using Bakalauras.Auth;
using Bakalauras.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace Bakalauras.data.entities
{
    public class Comment : IUserOwnedResource
    {
        public int Id { get; set; }

        public int EntityId { get; set; }

        public DateTime Date { get; set; }

        public string Content { get; set; }

        public string EntityType { get; set; }

        [Required]
        public string UserId { get; set; }
        public string Username { get; set; }
        public BookieUser User { get; set; }

    }
}
