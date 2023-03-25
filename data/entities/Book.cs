using Bakalauras.Auth;
using Bakalauras.Auth.Model;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Bakalauras.data.entities;

public class Book : IUserOwnedResource
{
    public int Id { get; set; }
    public string GenreName { get; set; }
    [Required]
    public string UserId { get; set; }

    public string Name { get; set; }

    public double Price { get; set; }

    public string Description { get; set; }

    public int PaymentPeriodDays { get; set; }
    public DateTime Created { get; set; }

    public virtual ICollection<Chapter>? Chapters { get; set; }

    public virtual ICollection<Comment>? Comments { get; set; }

    public virtual ICollection<ProfileBook>? ProfileBooks { get; set; }

   public BookieUser User { get; set; }

}
