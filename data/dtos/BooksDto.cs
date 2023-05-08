using Bakalauras.data.entities;

namespace Bakalauras.data.dtos
{
    public record BookDtoToBuy(int Id,string Name, string GenreName, string Description,double BookPrice,
        double ChapterPrice,int chapterCount,DateTime Created,string UserId,string Author,string CoverImageUrl,int IsFinished);
    public record BookDtoBought(int Id, string Name, ICollection<Chapter>? Chapters, string GenreName, string Description,
        double chapterPrice, double Price, DateTime Created, string UserId,string Author, string CoverImageUrl, int IsFinished,Status status,
        string statusMessage);
    public record CreateBookDto(string Name, string Description, string ChapterPrice,string BookPrice,IFormFile CoverImage);
    public record UpdateBookDto(int BookId,string GenreName,string Name,string Description,double ChapterPrice,
        double BookPrice, IFormFile coverImage);
    public record SubscribeToBookDto(int BookId, string GenreName,string Name, double Price,string Description,
                                    ICollection<Chapter>? Chapters, ICollection<Comment>? Comments);


}
