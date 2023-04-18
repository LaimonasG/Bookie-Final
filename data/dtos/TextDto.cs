namespace Bakalauras.data.dtos
{
    public record TextDtoBought(int Id,string Name, string GenreName, string Content, double Price, DateTime Created, string UserId);
    public record TextDtoToBuy(int Id, string Name, string GenreName, double Price, DateTime Created, string UserId);
        public record CreateTextDto(string Name, string Content, double Price);
        public record UpdateTextDto(string Name, string Content, double Price);
}
