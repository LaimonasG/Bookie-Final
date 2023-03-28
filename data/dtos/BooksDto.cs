using Bakalauras.data.entities;

namespace Bakalauras.data.dtos
{
    public record BookDto(int Id,string Name, string GenreName, string Description, double Price,
        DateTime Created,string UserId);
    public record CreateBookDto(string Name, string Description, double Price, DateTime Created);
    public record UpdateBookDto(string Name,string Description,double Price,DateTime Created);
    public record SubscribeToBookDto(int BookId, string GenreName,string Name, double Price,string Description,
                                    ICollection<Chapter>? Chapters, ICollection<Comment>? Comments);

}
