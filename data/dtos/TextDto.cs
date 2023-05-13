using Bakalauras.data.entities;

namespace Bakalauras.data.dtos
{
    public record TextDtoBought(int Id, string Name, string GenreName, string Content, string Description, double Price,
        string CoverImageUrl, string Author, DateTime Created, string UserId, Status status,
        string statusMessage);
    public record TextDtoToBuy(int Id, string Name, string GenreName, string Description, double Price, string CoverImageUrl,
        string Author, DateTime Created, string UserId);
    public record TextDto(string Name, string GenreName, string Content, string Description, double Price, string CoverImageUrl,
        DateTime Created);
    public record CreateTextDto(IFormFile File, IFormFile CoverImage, string Name, string Price, string Description);
    public record UpdateTextDto(IFormFile File, IFormFile CoverImage, string Name, string Price, string Description);
}
