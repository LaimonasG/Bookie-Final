using Bakalauras.data.entities;

namespace Bakalauras.data.dtos
{
    public record PersonalInfoDto(string? userName,string? email, string? Name, string? Surname);

    public record AuthorInfoDto(string? Name, string? Surname);
    public record CreateProfileDto(string userId);
    public record UpdateProfilePointsDto(int Points);
    public record ProfileDto(string userId, string userName, string email, double points);
    public record ProfileBooksDto(string userId, string userName,double Points, List<SubscribeToBookDto> books);

    public record ProfileTextsDto(string userId, string userName, List<Text> texts);

    public record ProfileBookOffersDto(int bookId,List<int> MissingChapters);

    public record ProfilePayDto(int bookId);
    public record ProfilePurchacesDto(List<Tuple<int, int>> BookPayments, List<Tuple<int, int>> TextPayments);

    public record ProfileBookPaymentDto(int bookId, double paymentAmount);
}
