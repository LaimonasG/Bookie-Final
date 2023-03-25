using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Bakalauras.Auth.Model;
using Bakalauras.Auth;

namespace Bakalauras.data.entities
{
    public class Text: IUserOwnedResource
    {
        public int Id { get; set; }
        public string GenreName { get; set; }
        [Required]
        public string UserId { get; set; }

        public string Name { get; set; }

        public double Price { get; set; }

        public string Content { get; set; }

        public virtual ICollection<ProfileText>? ProfileTexts { get; set; }

        public virtual ICollection<Comment>? Comments { get; set; }

        public DateTime Created { get; set; }

        public BookieUser User { get; set; }
    }
}
