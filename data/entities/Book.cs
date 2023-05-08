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

    public string? Author { get; set; }

    public string Name { get; set; }

    public double ChapterPrice { get; set; }

    public double BookPrice { get; set; }

    public string Description { get; set; }

    public int IsFinished { get; set; }

    public string? CoverImagePath { get; set; }

    public DateTime Created { get; set; }

    public Status Status { get; set; }

    public string? StatusComment { get; set; }

    public virtual ICollection<Chapter>? Chapters { get; set; }

    public virtual ICollection<Comment>? Comments { get; set; }

   public BookieUser User { get; set; }

}

public enum Status
{
    Pateikta,
    Patvirtinta,
    Atmesta
}
