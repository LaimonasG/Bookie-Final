namespace Bakalauras.data.dtos
{
        public record TextDto(int Id, string Name, string GenreName, string Content, double Price, DateTime Created, string UserId);
        public record CreateTextDto(string Name, string Content, double Price);
        public record UpdateTextDto(string Name, string Content, double Price);
        public record PurchaseTextDto(string Name, string Content, double Price);
}
