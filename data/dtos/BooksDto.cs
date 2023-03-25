namespace Bakalauras.data.dtos
{
    public record BookDto(int Id,string Name, string GenreName, string Description, double Price, DateTime Created,string UserId);
    public record CreateBookDto(string Name, string Description, double Price);
    public record UpdateBookDto(string Name,string Description,double Price);
    public record SubscribeToBookDto(int bookId, string genreName);

}
