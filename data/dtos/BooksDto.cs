using Bakalauras.data.entities;

namespace Bakalauras.data.dtos
{
    public record BookDtoToBuy(int Id,string Name, string GenreName, string Description, double Price,
        DateTime Created,string UserId);
    public record BookDtoBought(int Id, string Name, ICollection<Chapter>? Chapters, string GenreName, string Description,
        double Price, DateTime Created, string UserId);
    public record CreateBookDto(string Name, string Description, double ChapterPrice,double BookPrice, DateTime Created);
    public record UpdateBookDto(string Name,string Description,DateTime Created);
    public record SubscribeToBookDto(int BookId, string GenreName,string Name, double Price,string Description,
                                    ICollection<Chapter>? Chapters, ICollection<Comment>? Comments);

}
